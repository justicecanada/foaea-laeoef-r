﻿using FOAEA3.Common.Brokers;
using FOAEA3.Common.Helpers;
using FOAEA3.Model;
using FOAEA3.Model.Enums;
using FOAEA3.Web.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FOAEA3.Web.Pages;

public class FoaeaPageModel : PageModel
{
    protected readonly ApiConfig ApiRoots;
    protected readonly APIBrokerHelper BaseAPIs;
    protected readonly APIBrokerHelper InterceptionAPIs;
    private IHttpContextAccessor ContextAccessor;

    public List<MessageData> ErrorMessage { get; set; }
    public List<MessageData> WarningMessage { get; set; }
    public List<MessageData> InfoMessage { get; set; }

    public FoaeaPageModel(IHttpContextAccessor httpContextAccessor, ApiConfig apiConfig)
    {
        ApiRoots = apiConfig;

        if ((httpContextAccessor is not null) && (httpContextAccessor.HttpContext is not null))
        {
            string submitter = httpContextAccessor.HttpContext.Session.GetString(SessionValue.SUBMITTER);
            string userName = httpContextAccessor.HttpContext.Session.GetString(SessionValue.USER_NAME);

            ContextAccessor = httpContextAccessor;

            BaseAPIs = new APIBrokerHelper(ApiRoots.FoaeaRootAPI, submitter, userName,
                                       getToken: GetToken, getRefreshedToken: GetRefreshedToken);
            InterceptionAPIs = new APIBrokerHelper(ApiRoots.FoaeaInterceptionRootAPI, submitter, userName,
                                       getToken: GetToken, getRefreshedToken: GetRefreshedToken);

        }
    }

    public string GetToken()
    {
        if ((ContextAccessor is not null) && (ContextAccessor.HttpContext is not null))
        {
            if (ContextAccessor.HttpContext.Session.Keys.Contains(SessionValue.TOKEN))
                return ContextAccessor.HttpContext.Session.GetString(SessionValue.TOKEN);
            else
                return null;
        }
        return null;
    }

    public async Task<string> GetRefreshedToken()
    {
        if ((ContextAccessor is not null) && (ContextAccessor.HttpContext is not null))
        {
            string currentToken = ContextAccessor.HttpContext.Session.GetString(SessionValue.TOKEN);
            string refreshToken = ContextAccessor.HttpContext.Session.GetString(SessionValue.REFRESH_TOKEN);

            string userName = TokenDataHelper.UserName(currentToken);
            string submitter = TokenDataHelper.SubmitterCode(currentToken);

            var apiHelper = new APIBrokerHelper(ApiRoots.FoaeaRootAPI, submitter, userName);
            var apiBroker = new LoginsAPIBroker(apiHelper, currentToken);

            var result = await apiBroker.RefreshTokenAsync(currentToken, refreshToken);

            HttpContext.Session.SetString(SessionValue.TOKEN, result.Token);
            HttpContext.Session.SetString(SessionValue.REFRESH_TOKEN, result.RefreshToken);

            return result.Token;
        }
        else
            return "";
    }

    public void SetDisplayMessages(MessageDataList messages)
    {
        if (messages.ContainsMessagesOfType(MessageType.Error))
            ErrorMessage = messages.GetMessagesForType(MessageType.Error);

        if (messages.ContainsMessagesOfType(MessageType.Warning))
            WarningMessage = messages.GetMessagesForType(MessageType.Warning);

        if (messages.ContainsMessagesOfType(MessageType.Information))
            InfoMessage = messages.GetMessagesForType(MessageType.Information);
    }
}