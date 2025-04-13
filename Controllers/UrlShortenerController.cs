using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using URLShortener.Services;
using URLShortener.Data;
using URLShortener.Models;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class UrlShortenerController : ControllerBase
{
    private readonly UrlShorteningService _urlShorteningService;
    private readonly ApplicationDbContext _context;

    public UrlShortenerController(
        UrlShorteningService urlShorteningService,
        ApplicationDbContext context)
    {
        _urlShorteningService = urlShorteningService;
        _context = context;
    }

    [HttpPost("shorten")]
    public async Task<IActionResult> ShortenUrl(ShortenUrlRequest request)
    {
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
        {
            return BadRequest("The specified URL is invalid.");
        }

        var existing = await _context.ShortenedUrls
            .FirstOrDefaultAsync(x => x.LongUrl == request.Url);

        if (existing != null)
        {
            return Ok(existing);
        }

        var code = await _urlShorteningService.GenerateUniqueCode();

        var shortenedUrl = new ShortenedUrl
        {
            Id = Guid.NewGuid(),
            LongUrl = request.Url,
            Code = code,
            ShortUrl = $"{Request.Scheme}://{Request.Host}/api/UrlShortener/{code}",
            CreatedOnUtc = DateTime.UtcNow,
            ClickCount = 0
        };

        _context.ShortenedUrls.Add(shortenedUrl);
        await _context.SaveChangesAsync();

        return Ok(shortenedUrl);
    }


    [HttpGet("{code}")]
    public async Task<IActionResult> RedirectToOriginalUrl(string code)
    {
        var shortenedUrl = await _urlShorteningService
            .GetShortenedUrl(code);

        if (shortenedUrl is null)
        {
            return NotFound();
        }

        await _urlShorteningService.IncrementClickCount(code);

        return Redirect(shortenedUrl.LongUrl);
    }

    [HttpGet("stats/{code}")]
    public async Task<IActionResult> GetUrlStats(string code)
    {
        var shortenedUrl = await _urlShorteningService
            .GetShortenedUrl(code);

        if (shortenedUrl is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            OriginalUrl = shortenedUrl.LongUrl,
            ShortUrl = shortenedUrl.ShortUrl,
            ClickCount = shortenedUrl.ClickCount,
            CreatedOn = shortenedUrl.CreatedOnUtc
        });
    }
}
public class ShortenUrlRequest
{
    public string Url { get; set; } = string.Empty;
}