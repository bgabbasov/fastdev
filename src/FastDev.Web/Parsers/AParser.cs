using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FastDev.Web.Domain;
using FastDev.Web.Persistence;
using Microsoft.Extensions.Logging;

namespace FastDev.Web.Parsers
{
    public class AParser
    {
        private readonly AContext _aContext;
        private readonly FileStore _fileStore;
        private readonly ILogger _logger;
        private readonly LinkedList<string> _messages;

        private int? _currentIndex;
        private bool _hasId;
        private Guid _id;
        private readonly string[] _files = new string[3];

        private async Task CheckIfChangedAndSave(int index)
        {
            if (_currentIndex.HasValue && index == _currentIndex.Value) return;
            if (_currentIndex.HasValue) await ValidateAndSave();
            _currentIndex = index;
        }
        private async Task ValidateAndSave()
        {
            if (_currentIndex != null)
            {
                // Validate
                var hasErrors = false;
                if (!_hasId)
                {
                    _messages.AddLast($"Parameter guid[{_currentIndex}] is not provided.");
                    hasErrors = true;
                }
                for (var i = 0; i < _files.Length; i++)
                {
                    if (_files[i] == null)
                    {
                        _messages.AddLast($"Parameter file{i + 1}[{_currentIndex}] is not provided.");
                        hasErrors = true;
                    }
                }

                // Write to DB
                if (hasErrors)
                {
                    // Remove created files
                    foreach (var filename in _files)
                    {
                        if (filename == null) continue;

                        try
                        {
                            _fileStore.Delete(filename);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex.Message);
                        }
                    }
                    _messages.AddLast($"Skipping object [{_currentIndex}]. See _messages above to get more information.");
                }
                else
                {
                    try
                    {
                        var a = _aContext.A.Find(_id);
                        if (a == null) // A with the same id does not exist in the DB
                        {
                            await _aContext.A.AddAsync(new A(_id, _files[0], _files[1], _files[2]));
                        }
                        else // A with the same id already exists in the DB
                        {
                            a.File1 = _files[0];
                            a.File2 = _files[1];
                            a.File3 = _files[2];
                            _messages.AddLast($"Object with id = {_id} was already presented in the databse and will be overwritten.");
                        }
                        await _aContext.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _messages.AddLast($"An error occured when saving [{_currentIndex}], id = {_id}. The object was not saved to the database.");
                        _logger.LogError(ex.Message);
                    }
                }

                // Reset state
                _hasId = false;
                _id = Guid.Empty;
                for (var i = 0; i < _files.Length; i++) _files[i] = null;
            }
        }

        public AParser(AContext aContext, FileStore fileStore, ILogger logger, LinkedList<string> messages)
        {
            _aContext = aContext;
            _fileStore = fileStore;
            _logger = logger;
            _messages = messages;
        }

        /// <summary>
        /// Call this function when a guid parameter had come
        /// </summary>
        /// <param name="index">index of parameter</param>
        /// <param name="id">parameter value</param>
        /// <returns></returns>
        public async Task NextId(int index, Guid id)
        {
            await CheckIfChangedAndSave(index);

            _id = id;
            _hasId = true;
        }

        /// <summary>
        /// Call this function when a file parameter had come
        /// </summary>
        /// <param name="index">index of parameter</param>
        /// <param name="fileNo">file number (1, 2 or 3)</param>
        /// <param name="stream">file stream</param>
        /// <returns></returns>
        public async Task NextFile(int index, int fileNo, Stream stream)
        {
            await CheckIfChangedAndSave(index);

            if (fileNo < 1 || fileNo > 3)
            {
                _messages.AddLast($"Unexpected file parameter file{fileNo} was provided. The parameter was skipped.");
                return;
            }

            if (_files[fileNo - 1] != null)
            {
                _messages.AddLast($"file{fileNo}[{index}] was presented twice. The first occurance will be used.");
            }

            try
            {
                _files[fileNo - 1] = await _fileStore.CreateAsync(stream);
            }
            catch (Exception ex)
            {
                _messages.AddLast($"An error occured when saving file{fileNo}[{index}].");
                _logger.LogError(ex.Message);
            }
        }

        public async Task Finish()
        {
            if (_currentIndex.HasValue) await ValidateAndSave();
        }
    }
}
