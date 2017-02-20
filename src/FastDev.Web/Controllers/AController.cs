using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FastDev.Web.Domain;
using FastDev.Web.Dto;
using FastDev.Web.Filters;
using FastDev.Web.Helpers;
using FastDev.Web.Parsers;
using FastDev.Web.Persistence;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace FastDev.Web.Controllers
{
    [Route("api/[controller]")]
    public class AController : Controller
    {
        private readonly AContext _aContext;
        private readonly FileStore _fileStore;
        private readonly ILogger<AController> _logger;

        private readonly int _bufferSize = 1024;

        // Get the default form options so that we can use them to set the default limits for
        // request body data
        private static readonly FormOptions DefaultFormOptions = new FormOptions();

        // Regular expression is used for parsing parameter names
        private static readonly Regex ParamParser = new Regex(@"^(?<field>(guid|file1|file2|file3))\[(?<index>\d+)\]$",
           RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.CultureInvariant);


        public AController(AContext context, FileStore fileStore, ILogger<AController> logger)
        {
            _aContext = context;
            _fileStore = fileStore;
            _logger = logger;
        }

        // GET api/values
        [HttpGet]
        public async Task<IActionResult> Get(int page = 1, int pagesize = 10)
        {
            if (page <= 0) ModelState.AddModelError("page", "Page should be positive integer number");
            if (pagesize <= 0) ModelState.AddModelError("pagesize", "Page size should be positive integer number");

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var totalpages = (await _aContext.A.CountAsync() + pagesize - 1) / pagesize;
            var list = await _aContext.A.Skip(pagesize * (page - 1)).Take(pagesize).ToListAsync();

            return Json(new AListDto
            {
                TotalPages = totalpages,
                Data = list
            });
        }

        // GET api/values/5
        [HttpGet("{id}/{fileNo:int}")]
        public async Task<IActionResult> Get(Guid id, int fileNo)
        {
            var a = await _aContext.A.FindAsync(id);

            if (a == null) return NotFound($"Object A with Id = {id} not found.");

            Stream stream = null;
            try
            {
                switch (fileNo)
                {
                    case 1: stream = _fileStore.Get(a.File1, FileMode.Open, FileAccess.Read); break;
                    case 2: stream = _fileStore.Get(a.File2, FileMode.Open, FileAccess.Read); break;
                    case 3: stream = _fileStore.Get(a.File3, FileMode.Open, FileAccess.Read); break;
                    default: ModelState.AddModelError("fileNo", "FileNo must be between 1 and 3."); break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return NotFound($"File{fileNo} of object A with Id = {id} cannot be opened");
            }
            if (!ModelState.IsValid) return BadRequest(ModelState);

            return new FileStreamResult(stream, "image/jpeg");
        }

        // POST api/values
        [HttpPost]
        [DisableFormValueModelBinding]
        public async Task<IActionResult> Post()
        {
            var messages = new LinkedList<string>();

            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                return BadRequest($"Expected a multipart request, but got {Request.ContentType}");
            }

            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(Request.ContentType),
                DefaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);

            var parser = new AParser(_aContext, _fileStore, _logger);
            MultipartSection section;
            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                ContentDispositionHeaderValue contentDisposition = null;
                if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out contentDisposition)) continue;

                var match = ParamParser.Match(HeaderUtilities.RemoveQuotes(contentDisposition.Name));
                if (!match.Success)
                {
                    messages.AddLast($"Unexpected parameter {contentDisposition.Name}. Expected guid[index], file1[index], file2[index], file3[index].");
                    continue;
                }

                var field = match.Groups["field"].Value;
                var index = int.Parse(match.Groups["index"].Value);

                if (string.Equals(field, "guid", StringComparison.OrdinalIgnoreCase))
                {
                    if (!MultipartRequestHelper.HasFormDataContentDisposition(contentDisposition))
                    {
                        messages.AddLast($"A form data content disposition is expected but was not provided for guid[{index}].");
                        continue;
                    }

                    var encoding = GetEncoding(section);
                    using (var streamReader = new StreamReader(
                        section.Body,
                        encoding,
                        detectEncodingFromByteOrderMarks: true,
                        bufferSize: _bufferSize,
                        leaveOpen: true))
                    {
                        // The value length limit is enforced by MultipartBodyLengthLimit
                        var value = await streamReader.ReadToEndAsync();
                        Guid id;
                        if (!Guid.TryParse(value, out id))
                        {
                            messages.AddLast($"Unable to parse guid[{index}].");
                            continue;
                        }
                        await parser.NextId(index, id);
                    }
                }
                else // file1, file2 or file3
                {
                    if (!MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                    {
                        messages.AddLast($"A file content disposition is expected but was not provided for {field}[{index}].");
                        continue;
                    }

                    var fileNo = field[4] - '0';
                    await parser.NextFile(index, fileNo, section.Body);
                }
            }
            await parser.Finish();

            return Json(messages);
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var a = await _aContext.A.FindAsync(id);
            if (a == null) return NotFound($"Object A with Id = {id} not found.");

            _aContext.A.Remove(a);
            await _aContext.SaveChangesAsync();

            return Ok();
        }

        private static Encoding GetEncoding(MultipartSection section)
        {
            MediaTypeHeaderValue mediaType;
            var hasMediaTypeHeader = MediaTypeHeaderValue.TryParse(section.ContentType, out mediaType);
            // UTF-7 is insecure and should not be honored. UTF-8 will succeed in 
            // most cases.
            if (!hasMediaTypeHeader || Encoding.UTF7.Equals(mediaType.Encoding))
            {
                return Encoding.UTF8;
            }
            return mediaType.Encoding;
        }
    }
}
