﻿using FileBroker.Business;
using FileBroker.Data;
using FileBroker.Model;
using FileBroker.Model.Interfaces;
using FOAEA3.Common.Brokers;
using FOAEA3.Common.Helpers;
using FOAEA3.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.IO;
using System.Text;

namespace FileBroker.API.Fed.LicenceDenial.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class FederalLicenceDenialFilesController : ControllerBase
    {
        [HttpGet("Version")]
        public ActionResult<string> GetVersion() => Ok("FederalLicenceDenialFiles API Version 1.4");

        //GET api/v1/TraceRequests?partnerId=RC
        [HttpGet("")]
        public IActionResult GetFile([FromQuery] string partnerId, [FromServices] IFileTableRepository fileTable)
        {
            string fileName = partnerId + "3SLSOL"; // e.g. PA3SLSOL

            int fileCycleLength = 6; // TODO: should come from FileTable

            string fileContent = LoadLatestFederalLicenceDenialFile(fileName, fileTable, fileCycleLength, out string lastFileCycleString);

            if (fileContent == null)
                return NotFound();

            byte[] result = Encoding.UTF8.GetBytes(fileContent);

            return File(result, "text/xml", fileName + "." + lastFileCycleString + ".XML");
        }

        private static string LoadLatestFederalLicenceDenialFile(string fileName, IFileTableRepository fileTable,
                                                                 int fileCycleLength, out string lastFileCycleString)
        {
            var fileTableData = fileTable.GetFileTableDataForFileName(fileName);
            var fileLocation = fileTableData.Path;
            int lastFileCycle = fileTableData.Cycle; // - 1;
                                                     //if (lastFileCycle < 1)
                                                     //{
                                                     //    // e.g. 10³ - 1 = 999
                                                     //    // e.g. 10⁶ - 1 = 999999
                                                     //    lastFileCycle = (int)Math.Pow(10, fileCycleLength) - 1;
                                                     //}

            var lifeCyclePattern = new string('0', fileCycleLength);
            lastFileCycleString = lastFileCycle.ToString(lifeCyclePattern);

            string fullFilePath = $"{fileLocation}{fileName}.{lastFileCycleString}.XML";
            if (System.IO.File.Exists(fullFilePath))
                return System.IO.File.ReadAllText(fullFilePath);
            else
                return null;

        }

        [HttpPost]
        public ActionResult ProcessLicenceDenialFile([FromQuery] string fileName,
                                                     [FromServices] IFileAuditRepository fileAuditDB,
                                                     [FromServices] IFileTableRepository fileTableDB,
                                                     [FromServices] IMailServiceRepository mailService,
                                                     [FromServices] IFlatFileSpecificationRepository flatFileSpecs,
                                                     [FromServices] IOptions<ProvincialAuditFileConfig> auditConfig,
                                                     [FromServices] IOptions<ApiConfig> apiConfig,
                                                     [FromHeader] string currentSubmitter,
                                                     [FromHeader] string currentSubject)
        {
            string flatFileContent;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                flatFileContent = reader.ReadToEndAsync().Result;
            }

            if (string.IsNullOrEmpty(fileName))
                return UnprocessableEntity("Missing fileName");

            if (fileName.ToUpper().EndsWith(".XML"))
                fileName = fileName[0..^4]; // remove .XML extension

            var apiHelper = new APIBrokerHelper(apiConfig.Value.FoaeaLicenceDenialRootAPI, currentSubmitter, currentSubject);
            var licenceDenialApplicationAPIs = new LicenceDenialApplicationAPIBroker(apiHelper);
            var licenceDenialTerminationApplicationAPIs = new LicenceDenialTerminationApplicationAPIBroker(apiHelper);
            var licenceDenialEventAPIs = new LicenceDenialEventAPIBroker(apiHelper);

            var apis = new APIBrokerList
            {
                LicenceDenialApplications = licenceDenialApplicationAPIs,
                LicenceDenialTerminationApplications = licenceDenialTerminationApplicationAPIs,
                LicenceDenialEvents = licenceDenialEventAPIs
            };

            var repositories = new RepositoryList
            {
                FlatFileSpecs = flatFileSpecs,
                FileAudit = fileAuditDB,
                FileTable = fileTableDB,
                MailServiceDB = mailService
            };

            var licenceDenialManager = new IncomingFederalLicenceDenialManager(apis, repositories);

            var fileNameNoCycle = Path.GetFileNameWithoutExtension(fileName);
            var fileTableData = fileTableDB.GetFileTableDataForFileName(fileNameNoCycle);
            if (!fileTableData.IsLoading)
            {
                licenceDenialManager.ProcessJsonFile(flatFileContent, fileName);
                return Ok("File processed.");
            }
            else
                return UnprocessableEntity("File was already loading?");
        }
    }
}