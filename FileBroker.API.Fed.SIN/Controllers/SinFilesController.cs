using FileBroker.Business;
using FileBroker.Data;
using FileBroker.Model;
using FileBroker.Model.Interfaces;
using FOAEA3.Common.Brokers;
using FOAEA3.Common.Helpers;
using FOAEA3.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.IO;
using System.Linq;
using System.Text;

namespace FileBroker.API.Fed.SIN.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SinFilesController : ControllerBase
{
    [HttpGet("Version")]
    public ActionResult<string> GetVersion() => Ok("SinFiles API Version 1.0");

    [HttpGet("DB")]
    public ActionResult<string> GetDatabase([FromServices] IFileTableRepository fileTable) => Ok(fileTable.MainDB.ConnectionString);

    [HttpGet]
    public IActionResult GetFile([FromServices] IFileTableRepository fileTable)
    {
        string fileContent = LoadLatestFederalSinFile(fileTable, out string lastFileName);

        if (fileContent == null)
            return NotFound();

        byte[] result = Encoding.UTF8.GetBytes(fileContent);

        return File(result, "text/plain", lastFileName);
    }

    private static string LoadLatestFederalSinFile(IFileTableRepository fileTable, out string lastFileName)
    {
        var fileTableData = fileTable.GetFileTableDataForCategory("SINOUT")
                                     .FirstOrDefault(m => m.Active.HasValue && m.Active.Value);

        if (fileTableData is null)
        {
            lastFileName = "";
            return $"Error: fileTableData is empty for category SINOUT.";
        }

        var fileLocation = fileTableData.Path;
        int lastFileCycle = fileTableData.Cycle;

        int fileCycleLength = 3; // TODO: should come from FileTable

        var lifeCyclePattern = new string('0', fileCycleLength);
        string lastFileCycleString = lastFileCycle.ToString(lifeCyclePattern);
        lastFileName = $"{fileTableData.Name}.{lastFileCycleString}";

        string fullFilePath = $"{fileLocation}{lastFileName}";
        if (System.IO.File.Exists(fullFilePath))
            return System.IO.File.ReadAllText(fullFilePath);
        else
            return null;

    }

    [HttpPost]
    public ActionResult ProcessSINFile([FromQuery] string fileName,
                                       [FromServices] IFileAuditRepository fileAuditDB,
                                       [FromServices] IFileTableRepository fileTableDB,
                                       [FromServices] IMailServiceRepository mailService,
                                       [FromServices] IProcessParameterRepository processParameterDB,
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

        var apiHelper = new APIBrokerHelper(apiConfig.Value.FoaeaApplicationRootAPI, currentSubmitter, currentSubject);

        var apis = new APIBrokerList
        {
            Sins = new SinAPIBroker(apiHelper),
            ApplicationEvents = new ApplicationEventAPIBroker(apiHelper),
            Applications = new ApplicationAPIBroker(apiHelper)
        };

        var repositories = new RepositoryList
        {
            FlatFileSpecs = flatFileSpecs,
            FileAudit = fileAuditDB,
            FileTable = fileTableDB,
            MailServiceDB = mailService,
            ProcessParameterTable = processParameterDB
        };

        var sinManager = new IncomingFederalSinManager(apis, repositories);

        var fileNameNoCycle = Path.GetFileNameWithoutExtension(fileName);
        var fileTableData = fileTableDB.GetFileTableDataForFileName(fileNameNoCycle);
        if (!fileTableData.IsLoading)
        {
            var errors = sinManager.ProcessFlatFile(flatFileContent, fileName);
            if (errors.Any())
                return UnprocessableEntity(errors);
            else
                return Ok("File processed.");
        }
        else
            return UnprocessableEntity("File was already loading?");
    }
}
