using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace OctoshiftCLI;

public class LfsMigrator
{
    internal Func<string, string, Task> WriteToFile = async (path, contents) => await File.WriteAllTextAsync(path, contents);
    internal Func<string, string> ReadFile = fileName => File.ReadAllText(fileName);
    
    private readonly OctoLogger _log;
    private readonly ArchiveHandler _archiveHandler;

    public LfsMigrator(OctoLogger log, ArchiveHandler archiveHandler)
    {
        _log = log;
        _archiveHandler = archiveHandler;
    }

    public virtual async Task<Byte[]> LfsMigrate(byte[] gitArchiveContent)
    {
        _log.LogInformation("Migrating to lfs");
        
        var fileNames = _archiveHandler.Unpack(gitArchiveContent);

        // migrate to lfs

        _log.LogInformation("Done migrating to lfs");

        return _archiveHandler.Pack(_archiveHandler.extractDir);
    }
}