using FOAEA3.Model.Constants;
using System.Net.Http;
using System.Threading.Tasks;

namespace FOAEA3.Model.Interfaces
{
    public interface IAPIBrokerHelper
    {
        string APIroot { get; set; }
        string CurrentSubmitter { get; set; }
        string CurrentUser { get; set; }
        string CurrentLanguage { get; set; }

        MessageDataList Messages { get; set; }
        Task<T> GetDataAsync<T>(string api, string root = "") where T : class, new();
        Task<string> GetStringAsync(string api, string root = "", int maxAttempts = GlobalConfiguration.MAX_API_ATTEMPTS);
        Task<T> PostDataAsync<T, P>(string api, P data, string root = "") where T : class, new() where P : class;
        HttpResponseMessage PostJsonFile(string api, string jsonData, string rootAPI = null);
        HttpResponseMessage PostFlatFile(string api, string flatFileData, string rootAPI = null);
        Task<T> PutDataAsync<T, P>(string api, P data, string root = "") where T : class, new() where P : class;
        void SendCommand(string api, string root = "");
    }
}
