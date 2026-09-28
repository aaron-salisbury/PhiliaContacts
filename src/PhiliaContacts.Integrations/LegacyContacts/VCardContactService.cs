using PhiliaContacts.Business.Modules.Contacts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PhiliaContacts.Integrations.LegacyContacts;

// vCard 3.0 interchange. The parser accepts common 4.0 text fields and data-URI photos too.
internal sealed class VCardContactService : IVCardContactService
{
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
        string? given = null, middle = null, family = null, prefix = null, suffix = null;
        string? nickname = null, formattedName = null, phoneticGiven = null, phoneticFamily = null, title = null, organization = null, birthday = null, notes = null, url = null;
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
            string name = head.Split(';')[0].Split('.').Last().ToUpperInvariant();
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
                case "TEL": phones.Add(new(Unescape(raw).Replace("tel:", "", StringComparison.OrdinalIgnoreCase), Type(head))); break;
                case "EMAIL": emails.Add(new(Unescape(raw), Type(head))); break;
                case "ADR":
                    string[] address = SplitStructured(raw);
                    addresses.Add(new(Type(head), Unescape(address.ElementAtOrDefault(2)), Unescape(address.ElementAtOrDefault(3)),
                        Unescape(address.ElementAtOrDefault(4)), Unescape(address.ElementAtOrDefault(5)), Unescape(address.ElementAtOrDefault(6))));
                    break;
                case "PHOTO":
                    string encoded = raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ? raw[(raw.IndexOf(',') + 1)..] : raw;
                    if (encoded.Length > 12 * 1024 * 1024)
                    {
                        throw new InvalidDataException("Photo too large.");
                    }
                    try { photo = Convert.FromBase64String(encoded); }
                    catch (FormatException error) { throw new InvalidDataException("Invalid vCard photo.", error); }
                    break;
                case "BDAY": birthday = Unescape(raw); break;
                case "TITLE": title = Unescape(raw); break;
                case "ORG": organization = Unescape(raw); break;
                case "NOTE": notes = Unescape(raw); break;
                case "URL": url = Unescape(raw); break;
                default:
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

        if (photo?.Length > 8 * 1024 * 1024)
        {
            throw new InvalidDataException("Photo exceeds 8 MiB.");
        }

        return new Contact
        {
            Id = ContactId.New(),
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

            Value(output, "BDAY", contact.Birthday); Value(output, "TITLE", contact.Title);
            Value(output, "ORG", contact.Organization); Value(output, "URL", contact.Url); Value(output, "NOTE", contact.Notes);

            if (contact.Photo is { Length: > 0 } photo)
            {
                string format = photo.Length >= 3 && photo[0] == 0xff && photo[1] == 0xd8 ? "JPEG" : "PNG";
                Add(output, $"PHOTO;ENCODING=b;TYPE={format}:{Convert.ToBase64String(photo)}");
            }

            foreach (string extra in contact.VCardProperties)
            {
                if (extra.IndexOfAny(['\r', '\n']) < 0 && extra.IndexOf(':') > 0 && IsSafeExtra(extra[..extra.IndexOf(':')]))
                {
                    Add(output, extra);
                }
            }

            Add(output, "END:VCARD");
        }

        return output.ToString();
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
        return new string(type.Where(char.IsLetterOrDigit).ToArray()) is { Length: > 0 } safe ? safe : "OTHER";
    }

    private static bool IsSafeExtra(string head)
    {
        string key = head.Split(';')[0].Split('.').Last();
        return key.Length > 0 && key.All(character => char.IsLetterOrDigit(character) || character == '-');
    }

    private static string Type(string head)
    {
        string? parameter = head.Split(';').Skip(1).FirstOrDefault(part => part.StartsWith("TYPE=", StringComparison.OrdinalIgnoreCase));
        return parameter is null ? "Other" : parameter[5..].Split(',')[0];
    }

    private static string Escape(string? text)
    {
        return (text ?? "").Replace("\\", "\\\\").Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\\n").Replace(",", "\\,").Replace(";", "\\;");
    }

    private static string Unescape(string? text)
    {
        if (text is null)
        {
            return "";
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
        List<string> parts = [];
        StringBuilder part = new();

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\\' && i + 1 < text.Length)
            {
                part.Append(text[i++]); part.Append(text[i]);
            }
            else if (text[i] == ';')
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
                previous = (previous ?? "") + line[1..];
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
