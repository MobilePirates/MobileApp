/// <reference path="../../../../../typings/tsd.d.ts" />

module PracticePortal.Mobile.Core.Services.AuthorizationInterceptor {
    "use strict";

    var module = angular.module("PracticePortal.Mobile.Core.Services.AuthorizationInterceptor", []);

    module.factory('authInterceptor', ["sessionService", '$injector', "$window", "$q",
        ($ss, $injector, $window, $q: angular.IQService) => {
            var sessionService: SessionService.ISessionService = $ss;
            var rety = -1;

            function prepareHeaders(config, token) {
                if (token != "")
                    if (!config.headers) {
                        config.headers = { Authorization: "Bearer " + token };
                    } else {
                        config.headers.Authorization = "Bearer " + token;
                    }
                return config;
            }
            return {
                request: (config: angular.IRequestConfig): angular.IPromise<any> => {
                    return prepareHeaders(config, sessionService.getAccessToken());
                },
                responseError: (response) => {
                    if (response.status !== 401) {
                        return $q.reject(response);
                    }

                    if (rety < 0) {
                        return $q.defer().promise;
                    }
                    sessionService.clearAccessToken();
                    rety--;
                    var http = $injector.get('$http');

                    return http(response.config)
                        .then(r => {
                            rety = 3;
                            return r;
                        });
                }

            };
        }]);
}


