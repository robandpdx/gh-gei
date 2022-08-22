using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using OctoshiftCLI.Contracts;

namespace OctoshiftCLI;

public class LfsMigrator
{   
    private readonly OctoLogger _log;
    private readonly ArchiveHandler _archiveHandler;
    private readonly ISourceGithubApiFactory _sourceGithubApiFactory;
    public const string LFS_MIGRATION_DIR = "./lfsMigration";
    public const string LFS_MAPPING_FILE = "lfs-mapping.csv";

    public LfsMigrator(OctoLogger log, ArchiveHandler archiveHandler, ISourceGithubApiFactory sourceGithubApiFactory)
    {
        _log = log;
        _archiveHandler = archiveHandler;
        _sourceGithubApiFactory = sourceGithubApiFactory;
    }

    public virtual async Task<string> LfsMigrate(string ghesApiUrl, string githubSourceOrg, string sourceRepo, string githubSourcePat, bool noSslVerify = false)
    {
        _log.LogInformation("Migrating to lfs");

        var ghesApi = noSslVerify ? _sourceGithubApiFactory.CreateClientNoSsl(ghesApiUrl, githubSourcePat) : _sourceGithubApiFactory.Create(ghesApiUrl, githubSourcePat);
        Directory.CreateDirectory(LFS_MIGRATION_DIR);

        var cloneUrl = await ghesApi.GetRepoCloneUrl(githubSourceOrg, sourceRepo);

        // clone the repo
        var psiClone = new ProcessStartInfo();
        psiClone.FileName = "git";
        psiClone.Arguments = $"clone {cloneUrl}";
        psiClone.WorkingDirectory = LFS_MIGRATION_DIR;
        psiClone.RedirectStandardOutput = true;
        psiClone.UseShellExecute = false;
        psiClone.CreateNoWindow = true;

        using var processClone = Process.Start(psiClone);

        processClone.WaitForExit();

        var outputClone = processClone.StandardOutput.ReadToEnd();

        // migrate to lfs
        var psiMigrate = new ProcessStartInfo();
        psiMigrate.FileName = "git";
        psiMigrate.Arguments = $"lfs migrate import --everything --object-map={LFS_MAPPING_FILE} --include=\"*.BA1,*.BMP,*.CAB,*.DSK,*.DSM,*.DSW,*.EXE,*.ICO,*.JPG,*.KB,*.MDF,*.MSM,*.PKG,*.PNG,*.RTF,*.WAV,*.ZIP,*.ape,*.apf,*.archive,*.asmx,*.avi,*.bak,*.bin,*.bmp,*.bz2,*.cab,*.chi,*.chm,*.dat,*.dll,*.doc,*.docx,*.dsw,*.eap,*.exe,*.flali,*.flimpfl,*.flprj,*.fltar,*.fltoc,*.flvar,*.fpsbak,*.gif,*.gz,*.hhk_backup,*.ico,*.img,*.jpeg,*.jpg,*.kb,*.ldf,*.lib,*.mchyph,*.mpj,*.msdn,*.msi,*.msm,*.ncb,*.nupkg,*.obj,*.pbxbtree,*.pch,*.pdb,*.pdf,*.png,*.ppt,*.pptx,*.pyd,*.rar,*.resx,*.rom,*.rpm,*.rtf,*.sdf,*.svg,*.swg,*.sys,*.tar,*.tgz,*.tlb,*.wrf,*.xlf,*.xz,*.zip,*.dmg\"";
        psiMigrate.WorkingDirectory = $"{LFS_MIGRATION_DIR}/{sourceRepo}";
        psiMigrate.RedirectStandardOutput = true;
        psiMigrate.UseShellExecute = false;
        psiMigrate.CreateNoWindow = true;

        using var processMigrate = Process.Start(psiMigrate);

        processMigrate.WaitForExit();

        var outputMigrate = processMigrate.StandardOutput.ReadToEnd();

        // create new repo in source system $"{sourceRepo}-lfs"
        await ghesApi.CreateRepo(githubSourceOrg, sourceRepo + "-lfs");
        var lfsCloneUrl = await ghesApi.GetRepoCloneUrl(githubSourceOrg, sourceRepo + "-lfs");

        // add lfs origin at new location
        var psiOrigin = new ProcessStartInfo();
        psiOrigin.FileName = "git";
        psiOrigin.Arguments = $"remote add lfsOrigin {lfsCloneUrl}";
        psiOrigin.WorkingDirectory = $"{LFS_MIGRATION_DIR}/{sourceRepo}";
        psiOrigin.RedirectStandardOutput = true;
        psiOrigin.UseShellExecute = false;
        psiOrigin.CreateNoWindow = true;

        using var processOrigin = Process.Start(psiOrigin);

        processOrigin.WaitForExit();

        var outputOrigin = processOrigin.StandardOutput.ReadToEnd();

        // push the lfs migrated repo to a new location
        var psiPush = new ProcessStartInfo();
        psiPush.FileName = "git";
        psiPush.Arguments = "push --force lfsOrigin";
        psiPush.WorkingDirectory = $"{LFS_MIGRATION_DIR}/{sourceRepo}";
        psiPush.RedirectStandardOutput = true;
        psiPush.UseShellExecute = false;
        psiPush.CreateNoWindow = true;

        using var processPush = Process.Start(psiPush);

        processPush.WaitForExit();

        var outputPush = processPush.StandardOutput.ReadToEnd();

        // push the lfs files to the new repo

        _log.LogInformation("Done migrating to lfs");

        return sourceRepo + "-lfs";
    }
}