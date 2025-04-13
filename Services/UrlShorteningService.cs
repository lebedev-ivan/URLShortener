using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.Tasks;
using System;
using URLShortener.Data;
using URLShortener.Models;

namespace URLShortener.Services
{
    public class UrlShorteningService
    {
        public const int NumberOfCharsInShortLink = 7;
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public UrlShorteningService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<string> GenerateUniqueCode()
        {
            var codeChars = new char[NumberOfCharsInShortLink];

            while (true)
            {
                for (var i = 0; i < NumberOfCharsInShortLink; i++)
                {
                    var randomIndex = Random.Shared.Next(Alphabet.Length);
                    codeChars[i] = Alphabet[randomIndex];
                }

                var code = new string(codeChars);

                if (!await _context.ShortenedUrls.AnyAsync(s => s.Code == code))
                {
                    return code;
                }
            }
        }

        public async Task<ShortenedUrl?> GetShortenedUrl(string code)
        {
            if (_cache.TryGetValue(code, out ShortenedUrl? cachedUrl))
            {
                return cachedUrl;
            }

            var shortenedUrl = await _context.ShortenedUrls
                .FirstOrDefaultAsync(s => s.Code == code);

            _cache.Set(code, shortenedUrl, TimeSpan.FromMinutes(30));

            return shortenedUrl;
        }

        public async Task IncrementClickCount(string code)
        {
            var shortenedUrl = await _context.ShortenedUrls
                .FirstOrDefaultAsync(s => s.Code == code);

            if (shortenedUrl != null)
            {
                shortenedUrl.ClickCount++;
                await _context.SaveChangesAsync();

                _cache.Set(code, shortenedUrl, TimeSpan.FromMinutes(30));
            }
        }
    }
}