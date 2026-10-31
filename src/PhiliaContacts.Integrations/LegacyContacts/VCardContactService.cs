using PhiliaContacts.Business.Modules.Contacts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PhiliaContacts.Integrations.LegacyContacts;

// vCard 3.0 interchange. The parser accepts common 4.0 text fields and data-URI photos too.
internal sealed class VCardContactService : IVCardContactService
{
    //TODO: This class is in the LegacyContacts namespace, but it does not pertain to the legacy JSON implementation.
    //TODO: We're throwing exceptions for non-extraordinary scenarios. Possibly just skip invalid vCards or return error data so caller can gracefully fallback and report to user.

    private const string FAVORITE_CATEGORY = "starred";

    // TYPE values that describe capabilities or preference rather than a user-facing label.
    private static readonly HashSet<string> _nonLabelTypes = new(StringComparer.OrdinalIgnoreCase) { "INTERNET", "PREF", "VOICE", "X400", "MSG", "TEXT" };

    public IReadOnlyList<Contact> Read(string vcf)
    {
        ArgumentNullException.ThrowIfNull(vcf);

        if (Encoding.UTF8.GetByteCount(vcf) > 32 * 1024 * 1024)
        {
            throw new InvalidDataException("vCard import exceeds 32 MiB.");
        }

        List<Contact> result = [];
        List<string>? lines = null;
        foreach (string line in Unfold(vcf))
        {
            if (line.Equals("BEGIN:VCARD", StringComparison.OrdinalIgnoreCase))
            {
                if (lines is not null)
                {
                    throw new InvalidDataException("Nested vCard.");
                }
                lines = [];
            }
            else if (line.Equals("END:VCARD", StringComparison.OrdinalIgnoreCase))
            {
                if (lines is null)
                {
                    throw new InvalidDataException("Unexpected vCard end.");
                }
                result.Add(Parse(lines));
                lines = null;
            }
            else if (lines is not null)
            {
                lines.Add(line);
            }
            else if (!string.IsNullOrWhiteSpace(line))
            {
                throw new InvalidDataException("Content outside a vCard.");
            }
        }

        if (lines is not null)
        {
            throw new InvalidDataException("Incomplete vCard.");
        }

        return result;
    }

    private static Contact Parse(List<string> lines)
    {
        string? given = null, middle = null, family = null, prefix = null, suffix = null, uid = null;
        string? nickname = null, formattedName = null, phoneticGiven = null, phoneticFamily = null, title = null, organization = null, birthday = null, notes = null, url = null;
        string? twitter = null, facebook = null, linkedIn = null;
        bool isFavorite = false;
        byte[]? photo = null;
        List<ContactValue> phones = [], emails = [];
        List<ContactAddress> addresses = [];
        List<string> extras = [];

        foreach (string line in lines)
        {
            int colon = line.IndexOf(':');

            if (colon < 0)
            {
                throw new InvalidDataException("Invalid vCard property.");
            }

            string head = line[..colon];
            string name = PropertyName(head).ToUpperInvariant();
            string raw = line[(colon + 1)..];

            if (head.Contains("ENCODING=QUOTED-PRINTABLE", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Quoted-printable vCard data is not supported.");
            }

            switch (name)
            {
                case "VERSION":
                    if (raw is not ("3.0" or "4.0"))
                    {
                        throw new InvalidDataException("Only vCard 3.0 and 4.0 are supported.");
                    }
                    break;
                case "N":
                    string[] parts = SplitStructured(raw);
                    family = Unescape(parts.ElementAtOrDefault(0)); given = Unescape(parts.ElementAtOrDefault(1));
                    middle = Unescape(parts.ElementAtOrDefault(2)); prefix = Unescape(parts.ElementAtOrDefault(3)); suffix = Unescape(parts.ElementAtOrDefault(4));
                    break;
                case "FN":
                    formattedName = Unescape(raw);
                    break;
                case "NICKNAME": nickname = Unescape(raw); break;
                case "X-PHONETIC-FIRST-NAME": case "X-PHONETIC-GIVEN-NAME": phoneticGiven = Unescape(raw); break;
                case "X-PHONETIC-LAST-NAME": case "X-PHONETIC-FAMILY-NAME": phoneticFamily = Unescape(raw); break;
                case "CATEGORIES":
                    List<string> categories = [.. SplitList(raw)];
                    if (categories.RemoveAll(category => Unescape(category).Trim().Equals(FAVORITE_CATEGORY, StringComparison.OrdinalIgnoreCase)) > 0)
                    {
                        isFavorite = true;
                    }
                    if (categories.Count > 0 && IsSafeExtra(head))
                    {
                        extras.Add(head + ":" + string.Join(',', categories));
                    }
                    break;
                case "TEL": phones.Add(new(Unescape(raw).Replace("tel:", string.Empty, StringComparison.OrdinalIgnoreCase), Type(head))); break;
                case "EMAIL": emails.Add(new(Unescape(raw), Type(head))); break;
                case "ADR":
                    string[] address = SplitStructured(raw);
                    addresses.Add(new(Type(head), Unescape(address.ElementAtOrDefault(2)), Unescape(address.ElementAtOrDefault(3)),
                        Unescape(address.ElementAtOrDefault(4)), Unescape(address.ElementAtOrDefault(5)), Unescape(address.ElementAtOrDefault(6))));
                    break;
                case "PHOTO":
                    // Remote photo URLs (e.g. Google Contacts exports) and undecodable photos are skipped rather than failing the import.
                    photo = TryReadPhoto(head, raw) ?? photo;
                    break;
                case "BDAY": birthday = ContactBirthday.Normalize(Unescape(raw)); break;
                case "TITLE": title = Unescape(raw); break;
                case "ORG": organization = Unescape(raw); break;
                case "NOTE": notes = Unescape(raw); break;
                case "URL": url = Unescape(raw); break;
                case "X-SOCIALPROFILE":
                    string? service = Parameters(head).FirstOrDefault(parameter => parameter.Name == "TYPE").Value?.ToLowerInvariant();
                    string? user = SocialUser(head, Unescape(raw));
                    switch (service)
                    {
                        case "twitter" or "x" when user is not null: twitter = user; break;
                        case "facebook" when user is not null: facebook = user; break;
                        case "linkedin" when user is not null: linkedIn = user; break;
                        default:
                            if (IsSafeExtra(head))
                            {
                                extras.Add(line);
                            }
                            break;
                    }
                    break;
                case "X-TWITTER": twitter = Unescape(raw); break;
                case "X-FACEBOOK": facebook = Unescape(raw); break;
                case "X-LINKEDIN": linkedIn = Unescape(raw); break;
                default:
                    if (name == "UID")
                    {
                        uid = Unescape(raw).Trim();
                    }

                    if (name is not ("BEGIN" or "END") && IsSafeExtra(head))
                    {
                        extras.Add(line);
                    }
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(given) && string.IsNullOrWhiteSpace(family) && string.IsNullOrWhiteSpace(nickname))
        {
            nickname = formattedName;
        }

        if (string.IsNullOrWhiteSpace(given) && string.IsNullOrWhiteSpace(family) && string.IsNullOrWhiteSpace(nickname))
        {
            throw new InvalidDataException("A vCard requires a name.");
        }

        return new Contact
        {
            Id = IdFromUid(uid),
            IsFavorite = isFavorite,
            GivenName = given,
            MiddleName = middle,
            FamilyName = family,
            Prefix = prefix,
            Suffix = suffix,
            Nickname = nickname,
            PhoneticGivenName = phoneticGiven,
            PhoneticFamilyName = phoneticFamily,
            Title = title,
            Organization = organization,
            Birthday = birthday,
            Notes = notes,
            Url = url,
            TwitterUser = twitter,
            FacebookUser = facebook,
            LinkedInUser = linkedIn,
            Photo = photo,
            PhoneNumbers = phones,
            EmailAddresses = emails,
            Addresses = addresses,
            VCardProperties = extras
        };
    }

    public string Write(IReadOnlyList<Contact> contacts)
    {
        ArgumentNullException.ThrowIfNull(contacts);

        StringBuilder output = new();
        foreach (Contact contact in contacts)
        {
            Add(output, "BEGIN:VCARD"); Add(output, "VERSION:3.0");
            Add(output, $"N:{Escape(contact.FamilyName)};{Escape(contact.GivenName)};{Escape(contact.MiddleName)};{Escape(contact.Prefix)};{Escape(contact.Suffix)}");
            Add(output, $"FN:{Escape(contact.DisplayName)}");
            Value(output, "NICKNAME", contact.Nickname);
            Value(output, "X-PHONETIC-FIRST-NAME", contact.PhoneticGivenName);
            Value(output, "X-PHONETIC-LAST-NAME", contact.PhoneticFamilyName);

            List<string> extras = [.. contact.VCardProperties.Where(extra =>
                extra.IndexOfAny(['\r', '\n']) < 0 && extra.IndexOf(':') > 0 && IsSafeExtra(extra[..extra.IndexOf(':')]))];

            // Keep a single identity across exports so a re-import updates rather than duplicates.
            if (!extras.Any(extra => PropertyName(extra[..extra.IndexOf(':')]).Equals("UID", StringComparison.OrdinalIgnoreCase)))
            {
                Add(output, $"UID:urn:uuid:{contact.Id.Value:D}");
            }

            if (contact.IsFavorite)
            {
                int categoriesIndex = extras.FindIndex(extra => PropertyName(extra[..extra.IndexOf(':')]).Equals("CATEGORIES", StringComparison.OrdinalIgnoreCase));
                if (categoriesIndex < 0)
                {
                    Add(output, $"CATEGORIES:{FAVORITE_CATEGORY}");
                }
                else
                {
                    string categories = extras[categoriesIndex];
                    int colon = categories.IndexOf(':');
                    extras[categoriesIndex] = $"{categories[..colon]}:{FAVORITE_CATEGORY},{categories[(colon + 1)..]}";
                }
            }

            foreach (ContactValue item in contact.PhoneNumbers)
            {
                Value(output, $"TEL;TYPE={SafeType(item.Type)}", item.Value);
            }

            foreach (ContactValue item in contact.EmailAddresses)
            {
                Value(output, $"EMAIL;TYPE={SafeType(item.Type)}", item.Value);
            }

            foreach (ContactAddress address in contact.Addresses)
            {
                Add(output, $"ADR;TYPE={SafeType(address.Type)}:;;{Escape(address.Street)};{Escape(address.City)};{Escape(address.Region)};{Escape(address.PostalCode)};{Escape(address.Country)}");
            }

            Value(output, "BDAY", ContactBirthday.Normalize(contact.Birthday)); Value(output, "TITLE", contact.Title);
            Value(output, "ORG", contact.Organization); Value(output, "NOTE", contact.Notes);

            // URL is a URI value type, not TEXT, so it is written without TEXT escaping.
            if (!string.IsNullOrWhiteSpace(contact.Url))
            {
                Add(output, "URL:" + contact.Url.Trim().Replace("\r", string.Empty).Replace("\n", string.Empty));
            }

            SocialProfile(output, "twitter", "https://x.com/", contact.TwitterUser);
            SocialProfile(output, "facebook", "https://www.facebook.com/", contact.FacebookUser);
            SocialProfile(output, "linkedin", "https://www.linkedin.com/in/", contact.LinkedInUser);

            if (contact.Photo is { Length: > 0 } photo)
            {
                string? format = IsJpeg(photo) ? "JPEG" : IsPng(photo) ? "PNG" : null;
                Add(output, format is null
                    ? $"PHOTO;ENCODING=b:{Convert.ToBase64String(photo)}"
                    : $"PHOTO;ENCODING=b;TYPE={format}:{Convert.ToBase64String(photo)}");
            }

            foreach (string extra in extras)
            {
                Add(output, extra);
            }

            Add(output, "END:VCARD");
        }

        return output.ToString();
    }

    private static byte[]? TryReadPhoto(string head, string raw)
    {
        bool isUri = Parameters(head).Any(parameter => parameter.Name == "VALUE" && parameter.Value.Equals("uri", StringComparison.OrdinalIgnoreCase));
        bool isDataUri = raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase);

        if ((isUri && !isDataUri) || raw.Contains("://", StringComparison.Ordinal))
        {
            return null;
        }

        string encoded = isDataUri ? raw[(raw.IndexOf(',') + 1)..] : raw;
        if (encoded.Length > 12 * 1024 * 1024)
        {
            return null;
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(encoded);
        }
        catch (FormatException)
        {
            return null;
        }

        // Formats the app cannot preview are still kept, so their bytes survive a later export.
        return bytes.Length is > 0 and <= 8 * 1024 * 1024 ? bytes : null;
    }

    private static bool IsPng(byte[] data)
    {
        return data.Length >= 8 && data.AsSpan(0, 8).SequenceEqual((ReadOnlySpan<byte>)[137, 80, 78, 71, 13, 10, 26, 10]);
    }

    private static bool IsJpeg(byte[] data)
    {
        return data.Length >= 3 && data[0] == 0xff && data[1] == 0xd8 && data[2] == 0xff;
    }

    /// <summary>
    /// Uses the vCard UID as the contact identity so re-importing the same card updates the existing contact.
    /// GUID UIDs (including <c>urn:uuid:</c>) map directly; other UIDs map to a stable hash-derived GUID.
    /// </summary>
    private static ContactId IdFromUid(string? uid)
    {
        if (string.IsNullOrWhiteSpace(uid))
        {
            return ContactId.New();
        }

        string candidate = uid.StartsWith("urn:uuid:", StringComparison.OrdinalIgnoreCase) ? uid[9..] : uid;
        if (Guid.TryParse(candidate, out Guid guid) && guid != Guid.Empty)
        {
            return new ContactId(guid);
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes("vcard-uid:" + uid));
        return new ContactId(new Guid(hash.AsSpan(0, 16)));
    }

    private static void SocialProfile(StringBuilder output, string service, string baseUrl, string? user)
    {
        if (string.IsNullOrWhiteSpace(user))
        {
            return;
        }

        string trimmed = user.Trim().TrimStart('@');
        string parameterValue = new([.. trimmed.Where(character => char.IsLetterOrDigit(character) || character is '.' or '_' or '-')]);
        string parameter = parameterValue.Length > 0 ? $";X-USER={parameterValue}" : string.Empty;
        Add(output, $"X-SOCIALPROFILE;TYPE={service}{parameter}:{baseUrl}{Uri.EscapeDataString(trimmed)}");
    }

    private static string? SocialUser(string head, string value)
    {
        string? user = Parameters(head).FirstOrDefault(parameter => parameter.Name == "X-USER").Value;
        if (!string.IsNullOrWhiteSpace(user))
        {
            return user;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) && uri.Scheme is "http" or "https")
        {
            string? segment = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            return segment is null ? null : Uri.UnescapeDataString(segment);
        }

        int colon = value.IndexOf(':');
        string bare = colon >= 0 ? value[(colon + 1)..] : value;
        return string.IsNullOrWhiteSpace(bare) ? null : bare.Trim();
    }

    private static void Value(StringBuilder output, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            Add(output, key + ":" + Escape(value));
        }
    }

    private static string SafeType(string type)
    {
        return new string([.. type.Where(char.IsLetterOrDigit)]) is { Length: > 0 } safe ? safe : "OTHER";
    }

    private static string PropertyName(string head)
    {
        return head.Split(';')[0].Split('.').Last();
    }

    private static bool IsSafeExtra(string head)
    {
        string key = PropertyName(head);
        return key.Length > 0 && key.All(character => char.IsLetterOrDigit(character) || character == '-');
    }

    /// <summary>
    /// Parameters as upper-cased name/value pairs, one per comma-separated value. Bare vCard 2.1-style
    /// parameters (e.g. <c>TEL;CELL</c>) are treated as TYPE values.
    /// </summary>
    private static IEnumerable<(string Name, string Value)> Parameters(string head)
    {
        foreach (string parameter in head.Split(';').Skip(1))
        {
            int equals = parameter.IndexOf('=');
            string parameterName = equals < 0 ? "TYPE" : parameter[..equals].Trim().ToUpperInvariant();
            string values = equals < 0 ? parameter : parameter[(equals + 1)..];

            foreach (string value in values.Split(','))
            {
                string trimmed = value.Trim().Trim('"');
                if (trimmed.Length > 0)
                {
                    yield return (parameterName, trimmed);
                }
            }
        }
    }

    private static string Type(string head)
    {
        string[] types = [.. Parameters(head).Where(parameter => parameter.Name == "TYPE").Select(parameter => parameter.Value)];
        string? label = types.FirstOrDefault(type => !_nonLabelTypes.Contains(type)) ?? types.FirstOrDefault();

        return label is null ? "Other" : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(label.ToLowerInvariant());
    }

    private static string Escape(string? text)
    {
        return (text ?? string.Empty).Replace("\\", "\\\\").Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\\n").Replace(",", "\\,").Replace(";", "\\;");
    }

    private static string Unescape(string? text)
    {
        if (text is null)
        {
            return string.Empty;
        }

        StringBuilder result = new();
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\\' && i + 1 < text.Length)
            {
                char next = text[++i];
                result.Append(next is 'n' or 'N' ? '\n' : next);
            }
            else
            {
                result.Append(text[i]);
            }
        }

        return result.ToString();
    }

    private static string[] SplitStructured(string text)
    {
        return Split(text, ';');
    }

    private static string[] SplitList(string text)
    {
        return [.. Split(text, ',').Where(part => part.Trim().Length > 0)];
    }

    // Splits on an unescaped separator, leaving escape sequences intact for Unescape.
    private static string[] Split(string text, char separator)
    {
        List<string> parts = [];
        StringBuilder part = new();

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\\' && i + 1 < text.Length)
            {
                part.Append(text[i++]); part.Append(text[i]);
            }
            else if (text[i] == separator)
            {
                parts.Add(part.ToString()); part.Clear();
            }
            else
            {
                part.Append(text[i]);
            }
        }

        parts.Add(part.ToString());

        return [.. parts];
    }

    private static IEnumerable<string> Unfold(string text)
    {
        string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        string? previous = null;

        foreach (string line in lines)
        {
            if (line.StartsWith(' ') || line.StartsWith('\t'))
            {
                previous = (previous ?? string.Empty) + line[1..];
            }
            else
            {
                if (previous is not null)
                {
                    yield return previous;
                }
                previous = line;
            }
        }

        if (previous is not null)
        {
            yield return previous;
        }
    }

    private static void Add(StringBuilder output, string line)
    {
        int length = 0;
        foreach (Rune rune in line.EnumerateRunes())
        {
            if (length + rune.Utf8SequenceLength > 75)
            {
                output.Append("\r\n ");
                length = 1;
            }
            output.Append(rune.ToString());
            length += rune.Utf8SequenceLength;
        }
        output.Append("\r\n");
    }
}
