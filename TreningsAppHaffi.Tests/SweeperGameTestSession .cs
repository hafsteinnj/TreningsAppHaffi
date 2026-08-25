using Microsoft.AspNetCore.Http;

//generert wholesale av ChatGpt.
namespace TreningsAppHaffi.Tests
{
    internal class SweeperGameTestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _storage = new();

        public IEnumerable<string> Keys => _storage.Keys;

        public string Id => "TestSession";

        public bool IsAvailable => true;

        public void Clear()
        {
            _storage.Clear();
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            _storage.Remove(key);
        }

        public void Set(string key, byte[] value)
        {
            _storage[key] = value;
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            if (_storage.TryGetValue(key, out var storedValue))
            {
                value = storedValue;
                return true;
            }

            value = Array.Empty<byte>();
            return false;
            /*
             * Vet ikke hvorfor? 
             * Men jeg gikk litt amokk for å bli kvitt først en CS8767, warning.
             * som så ble til en cs8601 warning...
             * Ikke noe jeg ikke kunne latt være. Men jeg ville ha Error lista mi rein...
             */
        }
    }
}
