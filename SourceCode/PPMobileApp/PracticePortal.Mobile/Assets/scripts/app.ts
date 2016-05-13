/// <reference path="../../typings/tsd.d.ts" />


module PracticePortal.Mobile {
    "use strict";


    angular.module("PracticePortal.Mobile", ["PracticePortal.Mobile.Core"])
        .run(['$cordova', function ($cordova: Core.Services.ICordovaService) {
    }]);
}