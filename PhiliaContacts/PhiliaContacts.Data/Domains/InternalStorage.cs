using System;

namespace PhiliaContacts.Data.Domains
{
    [Serializable]
    internal class InternalStorage
    {
        public string UserStorageDirectory { get; set; } = null!;
    }
}
