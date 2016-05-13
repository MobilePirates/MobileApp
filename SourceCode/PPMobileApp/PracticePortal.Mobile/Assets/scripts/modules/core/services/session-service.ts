/// <reference path="../../../../../typings/tsd.d.ts" />

module PracticePortal.Mobile.Core.Services.SessionService {
    "use strict";

    var module = angular.module("PracticePortal.Mobile.Core.Services.SessionService", []);

    export interface ISessionService {
        token: string;
        getAccessToken(): string;
        clearAccessToken();
    }

    class SessionService implements ISessionService {
        token: string;

        constructor() {
            this.token = "";
        }

        public getAccessToken(): string {
            return this.token;
        }

        public clearAccessToken() {
            this.token = "";
        }

      
    }

    module.service('sessionService', () => new SessionService());
}


