﻿using Microsoft.Extensions.Configuration;

namespace FileBroker.Business.Helpers
{
    public class IncomingFederalSinFile
    {
        private RepositoryList DB { get; }
        private APIBrokerList FoaeaApis { get; }
        private IConfiguration Config { get; }

        public List<string> Errors { get; }

        public IncomingFederalSinFile(RepositoryList db,
                                      APIBrokerList foaeaApis,
                                      IConfiguration config)
        {
            DB = db;
            FoaeaApis = foaeaApis;
            Config = config;
            Errors = new List<string>();
        }

        public async Task AddNewFilesAsync(string rootPath, List<string> newFiles)
        {
            var directory = new DirectoryInfo(rootPath);
            var allFiles = directory.GetFiles("*IL.*");
            var last31days = DateTime.Now.AddDays(-31);
            var files = allFiles.Where(f => f.LastWriteTime > last31days).OrderByDescending(f => f.LastWriteTime);

            foreach (var fileInfo in files)
            {
                int cycle = FileHelper.GetCycleFromFilename(fileInfo.Name);
                var fileNameNoXmlExt = Path.GetFileNameWithoutExtension(fileInfo.Name); // remove xml extension 
                var fileNameNoCycle = Path.GetFileNameWithoutExtension(fileNameNoXmlExt); // remove cycle extension
                var fileTableData = await DB.FileTable.GetFileTableDataForFileNameAsync(fileNameNoCycle);

                if ((cycle == fileTableData.Cycle) && (fileTableData.Active.HasValue) && (fileTableData.Active.Value))
                    newFiles.Add(fileInfo.FullName);
            }
        }

        public async Task<bool> ProcessNewFileAsync(string fullPath)
        {
            string fileNameNoPath = Path.GetFileName(fullPath);

            if (fileNameNoPath?.ToUpper()[6] == 'I') // incoming file have a I in 7th position (e.g. HR3SVSIS.176)
            {                                        //                                                    ↑ 
                string flatFileContent = File.ReadAllText(fullPath);

                if (Errors.Any())
                    return false;

                var sinManager = new IncomingFederalSinManager(FoaeaApis, DB, Config);

                var fileNameNoCycle = Path.GetFileNameWithoutExtension(fileNameNoPath);
                var fileTableData = await DB.FileTable.GetFileTableDataForFileNameAsync(fileNameNoCycle);
                if (!fileTableData.IsLoading)
                {
                    await sinManager.ProcessFlatFileAsync(flatFileContent, fullPath);
                    return true;
                }
                else
                {
                    Errors.Add("File was already loading?");
                    return false;
                }

            }
            else
                Errors.Add($"Error: expected 'I' in 7th position, but instead found '{fileNameNoPath?.ToUpper()[6]}'. Is this an incoming file?");

            return false;
        }
    }
}