using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Gotify.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Gotify
{
    public class Notifier : INotificationService
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;

        public Notifier(IHttpClient httpClient, IJsonSerializer jsonSerializer)
        {
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
        }
        
        public async Task SendNotification(UserNotification request, CancellationToken cancellationToken)
        {
            var options = GetOptions(request.User);

            var body = new Dictionary<string, object>();
            
            // message parameter is required. If it is sent as null 
            // put name as message

            if (string.IsNullOrEmpty(request.Description))
            {
                body.Add("message", request.Name);
            }
            else
            {
                if (!string.IsNullOrEmpty(request.Name))
                    body.Add("title", request.Name);
                body.Add("message", request.Description);
            }

            body.Add("priority", options.Priority);

            var requestOptions = new HttpRequestOptions
            {
                Url = options.Url.TrimEnd('/') + $"/message?token={options.Token}",
                RequestContent = _jsonSerializer.SerializeToString(body),
                RequestContentType = "application/json",
                LogErrorResponseBody = true
            };

            await _httpClient.Post(requestOptions).ConfigureAwait(false);
        }

        public bool IsEnabledForUser(User user)
        {
            var options = GetOptions(user);
            return options != null && IsValid(options) && options.Enabled;
        }
        
        public string Name => Plugin.Instance.Name;

        private static GotifyOptions GetOptions(BaseItem user)
        {
            return Plugin.Instance.Configuration.Options
                .FirstOrDefault(u => string.Equals(u.UserId, user.Id.ToString("N"),
                    StringComparison.OrdinalIgnoreCase));
        }
        
        private static bool IsValid(GotifyOptions options)
        {
            return !string.IsNullOrEmpty(options.Token)
                   && !string.IsNullOrEmpty(options.Url);
        }
    }
}