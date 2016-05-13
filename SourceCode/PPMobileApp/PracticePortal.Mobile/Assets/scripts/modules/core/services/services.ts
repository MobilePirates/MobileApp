/// <reference path="../../../../../typings/tsd.d.ts" />

module PracticePortal.Mobile.Core.Services {
    "use strict";

    var module = angular.module("PracticePortal.Mobile.Core.Services",
        ["PracticePortal.Mobile.Core.Services.Base64", "PracticePortal.Mobile.Core.Services.SessionService", "PracticePortal.Mobile.Core.Services.AuthorizationInterceptor"]);

    module.config(['$httpProvider', httpProvider => {
        httpProvider.interceptors.push('authInterceptor');
    }]);

    export interface ICordovaService {
        loadConversations(skip: number, take: number, displayMessage: number, filter: string): angular.IPromise<Array<any>>;
        postImage(imageString: string);
        login(user: string, password: string): angular.IPromise<any> ;
    }

    class CordovaService implements ICordovaService {
        private $q: ng.IQService;
        private $injector: ng.auto.IInjectorService;
        private $http: ng.IHttpService;
        private $rootScope: ng.IScope;
        private events: Array<any>;
        private readyEventDefer;
        private whenReady: ng.IPromise<void>;
        private base64: Base64.IBase64;
        private sessionService: SessionService.ISessionService;

        constructor($q: ng.IQService, $injector: ng.auto.IInjectorService, $rootScope: ng.IScope, $http: ng.IHttpService, base64: Base64.IBase64, sessionService: SessionService.ISessionService) {
            this.events = new Array();

            this.$q = $q;
            this.$injector = $injector;
            this.$rootScope = $rootScope;
            this.$http = $http;
            this.base64 = base64;
            this.sessionService = sessionService;
            
            this.readyEventDefer = this.$q.defer();
            this.whenReady = this.readyEventDefer.promise;
            return this;
        }

        public login(user: string, password: string): angular.IPromise<any> {
            return this.$http({
                method: 'POST',
                url: "https://aaatest.wolterskluwercloud.com/auth/core/connect/token",
                data: this.generatePostData(user, password),
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'Authorization': this.generateAuthorizationHeader()
                }
            }).then(response => {
                if (response && response.data)
                {
                    var result: any;
                    result = response.data;
                    this.sessionService.token = result.access_token;
                    return result.access_token;
                }
                return "";
            }, e =>
                    this.$q.reject(e));
        }

        private generatePostData(user: string, password: string): string {
            var userName = encodeURIComponent(user);
            return "grant_type=password&username=" + userName + "&password=" + password + "&scope=WK.UK.PracticePortal.Access";
        }

        private generateAuthorizationHeader(): string {
            return 'Basic ' + this.encodeCredentials('WK.UK.PracticePortal.Mobile', '123456');
        }

        private encodeCredentials(name: string, password: string): string {
            var credential: string = name + ':' + password;
            
            return this.base64.encode(credential);
        }
        
        public loadConversations(skip: number, take: number, displayMessage: number, filter: string): angular.IPromise<Array<any>> {
            return this.$http({
                method: 'GET',
                url: 'http://cchppdev.azurewebsites.net/api/PracticePortal/conversations',
                params:
                {
                    take: take,
                    skip: skip,
                    previewLimit: displayMessage,
                    filter: filter
                },
                headers: {
                    'Accept': "text/html",
                    'Content-Type': "application/json",
                }
            }).then(response => {
                if (response && response.data)
                    return response.data;
                return [];
                }, e =>
                    this.$q.reject(e));
        
    }

        public postImage(imageString: string) {
         //https://oanad.cchportal.co.uk/v2016-1/pp/documents/download/{0}/{1}
            //"http://cchppdev.azurewebsites.net/api/api/practiceportal/documents",

            return this.$http.post("http://cchppdev.azurewebsites.net/api/api/practiceportal/documents", { imageString: imageString });
    }
}

    module.factory("$cordova", ["$q", "$injector", "$rootScope", "$http", 'oBase64', 'sessionService', ($q, $injector, $rootScope, $http, base64, sessionService) => new CordovaService($q, $injector, $rootScope, $http, base64, sessionService)]);
}