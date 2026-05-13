using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace OutWit.Common.Email.Templates
{
    /// <summary>
    /// Reads templates from disk under a configurable root directory. Each template
    /// name maps to <c>&lt;root&gt;/&lt;name&gt;.html</c>. Results are cached in memory
    /// with a sliding expiration when an <see cref="IMemoryCache"/> is provided.
    /// </summary>
    public class FileEmailTemplateProvider : IEmailTemplateProvider
    {
        #region Constants

        private const string CACHE_KEY_PREFIX = "email.tpl::";

        private static readonly TimeSpan DEFAULT_SLIDING_EXPIRATION = TimeSpan.FromMinutes(10);

        #endregion

        #region Fields

        private readonly string m_templatesRoot;

        private readonly IMemoryCache? m_cache;

        private readonly ILogger<FileEmailTemplateProvider> m_logger;

        private readonly TimeSpan m_slidingExpiration;

        #endregion

        #region Constructors

        public FileEmailTemplateProvider(string templatesRoot,
            IMemoryCache? cache = null,
            ILogger<FileEmailTemplateProvider>? logger = null,
            TimeSpan? slidingExpiration = null)
        {
            if (string.IsNullOrWhiteSpace(templatesRoot))
                throw new ArgumentException("Templates root must be a non-empty path.", nameof(templatesRoot));

            m_templatesRoot = templatesRoot;
            m_cache = cache;
            m_logger = logger ?? NullLogger<FileEmailTemplateProvider>.Instance;
            m_slidingExpiration = slidingExpiration ?? DEFAULT_SLIDING_EXPIRATION;
        }

        #endregion

        #region IEmailTemplateProvider

        public async Task<string> GetTemplateAsync(string name, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Template name must be a non-empty string.", nameof(name));

            var cacheKey = CACHE_KEY_PREFIX + name;
            if (m_cache != null && m_cache.TryGetValue(cacheKey, out string? cached))
                return cached ?? string.Empty;

            var path = Path.Combine(m_templatesRoot, name + ".html");
            if (!File.Exists(path))
            {
                m_logger.LogError("Email template not found, name={Name} path={Path}", name, path);
                throw new FileNotFoundException($"Email template '{name}' not found.", path);
            }

            string html;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
                html = await reader.ReadToEndAsync().ConfigureAwait(false);

            if (m_cache != null)
            {
                m_cache.Set(cacheKey, html, new MemoryCacheEntryOptions
                {
                    Priority = CacheItemPriority.High,
                    SlidingExpiration = m_slidingExpiration
                });
            }

            return html;
        }

        #endregion
    }
}
