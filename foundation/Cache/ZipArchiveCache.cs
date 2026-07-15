using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Concurrent;


namespace BizSrt.Foundation.Cache
{
    public class ZipArchiveCache
    {
        byte[] _buffer;
        ConcurrentDictionary<string, Tuple<byte[],DateTime>> _files;

        public ZipArchiveCache(byte[] buffer, string[] extractFiles = null)
        {
            _buffer = buffer;
            _files = new ConcurrentDictionary<string, Tuple<byte[], DateTime>>();

            if (extractFiles != null)
            {
                using (var zipArchive = openArchive())
                {
                    foreach(var fileName in extractFiles)
                    {
                        var file = extractFile(zipArchive, fileName);
                        if (file != null)
                            _files.TryAdd(fileName, file);
                    }
                }
            }
        }

        ZipArchive openArchive()
        {
            return new ZipArchive(new MemoryStream(this._buffer), ZipArchiveMode.Read);
        }

        Tuple<byte[], DateTime> extractFile(ZipArchive zipArchive, string fileName)
        {
            var entry = zipArchive.GetEntry(fileName);
            using (var entryStream = entry.Open())
            {
                using (var memoryStream = new MemoryStream())
                {
                    entryStream.CopyTo(memoryStream);
                    return new Tuple<byte[], DateTime>(memoryStream.ToArray(), entry.LastWriteTime.UtcDateTime);
                }
            }
        }

        public Tuple<byte[], DateTime> this[string fileName]
        {
            get
            {
                Tuple<byte[], DateTime> file;
                if (!_files.TryGetValue(fileName, out file))
                {
                    using (var zipArchive = openArchive())
                    {
                        file = extractFile(zipArchive, fileName);
                        if (file != null)
                            _files.TryAdd(fileName, file);
                    }
                }
                return file;
            }
        }
    }
}
