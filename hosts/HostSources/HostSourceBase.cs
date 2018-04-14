using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace hosts.HostSources
{
    public abstract class HostSourceBase : IDisposable
    {
        #region Fields
        private static HttpClient client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10d)
        };
        #endregion

        #region Properties
        private readonly Uri _uri = default;
        public Uri Uri => _uri;
        
        private bool _wasAvailable = false;
        public bool WasAvailable => _wasAvailable;
        #endregion
        
        protected HostSourceBase(Uri uri)
        {
            _uri = uri ?? throw new ArgumentNullException(nameof(uri));

            // if (client == null)
            // {
            //     client = new HttpClient
            //     {
            //         Timeout = TimeSpan.FromSeconds(10d)
            //     };
            // }
        }

        public static IEnumerable<HostSourceBase> AllSources()
        {
            // an easy way to get every currently available source

            yield return new MVPS();
            yield return new SANS(new Uri("https://isc.sans.edu/feeds/suspiciousdomains_Low.txt"));
            yield return new SANS(new Uri("https://isc.sans.edu/feeds/suspiciousdomains_Medium.txt"));
            yield return new SANS(new Uri("https://isc.sans.edu/feeds/suspiciousdomains_High.txt"));
            yield return new AbuseCH();
        }
        
        public async Task<Domain[]> GetDomainsAsync()
        {
            string text = await DownloadAsync().ConfigureAwait(false);

            if (String.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<Domain>();
            }

            string[] lines = await ReadLinesAsync(text).ConfigureAwait(false);

            var domains = new List<Domain>();

            foreach (string each in lines)
            {
                if (TryCreate(each, out Domain domain))
                {
                    domains.Add(domain);
                }
            }

            return domains.ToArray();
        }
        
        private async Task<string> DownloadAsync()
        {
            if (_uri == null) { throw new ArgumentNullException(nameof(_uri)); }
            
            string text = string.Empty;

            try
            {
                using (HttpResponseMessage resp = await client.GetAsync(_uri).ConfigureAwait(false))
                {
                    if (resp.IsSuccessStatusCode)
                    {
                        _wasAvailable = true;

                        text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        _wasAvailable = false;
                    }
                }
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) { }

            return text;
        }

        private async Task<string[]> ReadLinesAsync(string text)
        {
            var lines = new List<string>();

            string line = string.Empty;

            using (StringReader sr = new StringReader(text))
            {
                while ((line = await sr.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    lines.Add(line);
                }
            }

            return lines.ToArray();
        }


        protected abstract bool TryCreate(string line, out Domain domain);

        public override string ToString() => $"{GetType().Name} - {Uri.AbsoluteUri}";


        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    client.Dispose();
                }
                
                disposedValue = true;
            }
        }
        
        void IDisposable.Dispose()
        {
            Dispose(true);

            //GC.SuppressFinalize(this);
        }
        #endregion
    }
}
