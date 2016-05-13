module PracticePortal.Mobile.Core.Controllers {
    "use strict";

    var module = angular.module("PracticePortal.Mobile.Core.Controllers", []);

    interface IHomeScope extends ng.IScope {
        homeMessage: string;

        clickButton(): void;
    }

    class HomeController {
  
        constructor($scope: IHomeScope, $cordova: Services.ICordovaService) {
            $scope.homeMessage = "Any photos that will be taken will be uploaded to Mrs. Ana von Smith S and OCRed.";

            $cordova.login("ptrkpp@mailinator.com", "123456");

            $scope.clickButton = function () {

                navigator.camera.getPicture(
                    function (imageData) {
                        $cordova.postImage(imageData);
                    },
                    function (message) {
                    },
                    {
                        destinationType: 0
                    });

            }
 
        }
     
    }

    module.controller("homeController", ['$scope', '$cordova', ($scope, $cordova) => new HomeController($scope, $cordova)]);
}