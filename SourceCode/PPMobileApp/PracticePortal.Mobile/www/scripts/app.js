var PracticePortal;
(function (PracticePortal) {
    var Mobile;
    (function (Mobile) {
        "use strict";
        angular.module("PracticePortal.Mobile", ["PracticePortal.Mobile.Core"])
            .run(['$cordova', function ($cordova) {
            }]);
    })(Mobile = PracticePortal.Mobile || (PracticePortal.Mobile = {}));
})(PracticePortal || (PracticePortal = {}));
var PracticePortal;
(function (PracticePortal) {
    var Mobile;
    (function (Mobile) {
        var Core;
        (function (Core) {
            var Controllers;
            (function (Controllers) {
                "use strict";
                var module = angular.module("PracticePortal.Mobile.Core.Controllers", []);
                var HomeController = (function () {
                    function HomeController($scope, $cordova) {
                        $scope.homeMessage = "Any photos that will be taken will be uploaded to Mrs. Ana von Smith S and OCRed.";
                        $cordova.login("ptrkpp@mailinator.com", "123456");
                        $scope.clickButton = function () {
                            navigator.camera.getPicture(function (imageData) {
                                $cordova.postImage(imageData);
                            }, function (message) {
                            }, {
                                destinationType: 0
                            });
                        };
                    }
                    return HomeController;
                }());
                module.controller("homeController", ['$scope', '$cordova', function ($scope, $cordova) { return new HomeController($scope, $cordova); }]);
            })(Controllers = Core.Controllers || (Core.Controllers = {}));
        })(Core = Mobile.Core || (Mobile.Core = {}));
    })(Mobile = PracticePortal.Mobile || (PracticePortal.Mobile = {}));
})(PracticePortal || (PracticePortal = {}));
var PracticePortal;
(function (PracticePortal) {
    var Mobile;
    (function (Mobile) {
        var Core;
        (function (Core) {
            'uses strict';
            angular.module("PracticePortal.Mobile.Core", ["PracticePortal.Mobile.Core.Controllers", "PracticePortal.Mobile.Core.Services"]);
        })(Core = Mobile.Core || (Mobile.Core = {}));
    })(Mobile = PracticePortal.Mobile || (PracticePortal.Mobile = {}));
})(PracticePortal || (PracticePortal = {}));
var PracticePortal;
(function (PracticePortal) {
    var Mobile;
    (function (Mobile) {
        var Core;
        (function (Core) {
            var Services;
            (function (Services) {
                var AuthorizationInterceptor;
                (function (AuthorizationInterceptor) {
                    "use strict";
                    var module = angular.module("PracticePortal.Mobile.Core.Services.AuthorizationInterceptor", []);
                    module.factory('authInterceptor', ["sessionService", '$injector', "$window", "$q",
                        function ($ss, $injector, $window, $q) {
                            var sessionService = $ss;
                            var rety = -1;
                            function prepareHeaders(config, token) {
                                if (token != "")
                                    if (!config.headers) {
                                        config.headers = { Authorization: "Bearer " + token };
                                    }
                                    else {
                                        config.headers.Authorization = "Bearer " + token;
                                    }
                                return config;
                            }
                            return {
                                request: function (config) {
                                    return prepareHeaders(config, sessionService.getAccessToken());
                                },
                                responseError: function (response) {
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
                                        .then(function (r) {
                                        rety = 3;
                                        return r;
                                    });
                                }
                            };
                        }]);
                })(AuthorizationInterceptor = Services.AuthorizationInterceptor || (Services.AuthorizationInterceptor = {}));
            })(Services = Core.Services || (Core.Services = {}));
        })(Core = Mobile.Core || (Mobile.Core = {}));
    })(Mobile = PracticePortal.Mobile || (PracticePortal.Mobile = {}));
})(PracticePortal || (PracticePortal = {}));
var PracticePortal;
(function (PracticePortal) {
    var Mobile;
    (function (Mobile) {
        var Core;
        (function (Core) {
            var Services;
            (function (Services) {
                var Base64;
                (function (Base64_1) {
                    "use strict";
                    var module = angular.module("PracticePortal.Mobile.Core.Services.Base64", []);
                    var Base64 = (function () {
                        function Base64() {
                            this._keyStr = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";
                        }
                        Base64.prototype.encode = function (input) {
                            if (!input) {
                                throw Error("null or empty argument input");
                            }
                            var output = "";
                            var chr1, chr2, chr3, enc1, enc2, enc3, enc4;
                            var i = 0;
                            input = this._utf8_encode(input);
                            while (i < input.length) {
                                chr1 = input.charCodeAt(i++);
                                chr2 = input.charCodeAt(i++);
                                chr3 = input.charCodeAt(i++);
                                enc1 = chr1 >> 2;
                                enc2 = ((chr1 & 3) << 4) | (chr2 >> 4);
                                enc3 = ((chr2 & 15) << 2) | (chr3 >> 6);
                                enc4 = chr3 & 63;
                                if (isNaN(chr2)) {
                                    enc3 = enc4 = 64;
                                }
                                else if (isNaN(chr3)) {
                                    enc4 = 64;
                                }
                                output = output +
                                    this._keyStr.charAt(enc1) + this._keyStr.charAt(enc2) +
                                    this._keyStr.charAt(enc3) + this._keyStr.charAt(enc4);
                            }
                            return output;
                        };
                        Base64.prototype.decode = function (input) {
                            var output = "";
                            var chr1, chr2, chr3;
                            var enc1, enc2, enc3, enc4;
                            var i = 0;
                            var inoutMatch = /^([A-Za-z0-9+/]{4})*([A-Za-z0-9+/]{4}|[A-Za-z0-9+/]{3}=|[A-Za-z0-9+/]{2}==)$/;
                            if (!input) {
                                throw Error("null or empty argument input");
                            }
                            if (!inoutMatch.test(input)) {
                                throw Error("Invalid base 64 string");
                            }
                            input = input.replace(/[^A-Za-z0-9\+\/\=]/g, "");
                            while (i < input.length) {
                                enc1 = this._keyStr.indexOf(input.charAt(i++));
                                enc2 = this._keyStr.indexOf(input.charAt(i++));
                                enc3 = this._keyStr.indexOf(input.charAt(i++));
                                enc4 = this._keyStr.indexOf(input.charAt(i++));
                                chr1 = (enc1 << 2) | (enc2 >> 4);
                                chr2 = ((enc2 & 15) << 4) | (enc3 >> 2);
                                chr3 = ((enc3 & 3) << 6) | enc4;
                                output = output + String.fromCharCode(chr1);
                                if (enc3 != 64) {
                                    output = output + String.fromCharCode(chr2);
                                }
                                if (enc4 != 64) {
                                    output = output + String.fromCharCode(chr3);
                                }
                            }
                            output = this._utf8_decode(output);
                            return output;
                        };
                        Base64.prototype._utf8_encode = function ($string) {
                            $string = $string.replace(/\r\n/g, "\n");
                            var utftext = "";
                            for (var n = 0; n < $string.length; n++) {
                                var c = $string.charCodeAt(n);
                                if (c < 128) {
                                    utftext += String.fromCharCode(c);
                                }
                                else if ((c > 127) && (c < 2048)) {
                                    utftext += String.fromCharCode((c >> 6) | 192);
                                    utftext += String.fromCharCode((c & 63) | 128);
                                }
                                else {
                                    utftext += String.fromCharCode((c >> 12) | 224);
                                    utftext += String.fromCharCode(((c >> 6) & 63) | 128);
                                    utftext += String.fromCharCode((c & 63) | 128);
                                }
                            }
                            return utftext;
                        };
                        Base64.prototype._utf8_decode = function (utftext) {
                            var $string = "";
                            var i = 0;
                            var c = 0;
                            var c1 = 0;
                            var c2 = 0;
                            var c = c1 = c2 = 0;
                            while (i < utftext.length) {
                                c = utftext.charCodeAt(i);
                                if (c < 128) {
                                    $string += String.fromCharCode(c);
                                    i++;
                                }
                                else if ((c > 191) && (c < 224)) {
                                    c2 = utftext.charCodeAt(i + 1);
                                    $string += String.fromCharCode(((c & 31) << 6) | (c2 & 63));
                                    i += 2;
                                }
                                else {
                                    c2 = utftext.charCodeAt(i + 1);
                                    var c3 = utftext.charCodeAt(i + 2);
                                    $string += String.fromCharCode(((c & 15) << 12) | ((c2 & 63) << 6) | (c3 & 63));
                                    i += 3;
                                }
                            }
                            return $string;
                        };
                        return Base64;
                    }());
                    module.service('oBase64', function () { return new Base64(); });
                })(Base64 = Services.Base64 || (Services.Base64 = {}));
            })(Services = Core.Services || (Core.Services = {}));
        })(Core = Mobile.Core || (Mobile.Core = {}));
    })(Mobile = PracticePortal.Mobile || (PracticePortal.Mobile = {}));
})(PracticePortal || (PracticePortal = {}));
var PracticePortal;
(function (PracticePortal) {
    var Mobile;
    (function (Mobile) {
        var Core;
        (function (Core) {
            var Services;
            (function (Services) {
                "use strict";
                var module = angular.module("PracticePortal.Mobile.Core.Services", ["PracticePortal.Mobile.Core.Services.Base64", "PracticePortal.Mobile.Core.Services.SessionService", "PracticePortal.Mobile.Core.Services.AuthorizationInterceptor"]);
                module.config(['$httpProvider', function (httpProvider) {
                        httpProvider.interceptors.push('authInterceptor');
                    }]);
                var CordovaService = (function () {
                    function CordovaService($q, $injector, $rootScope, $http, base64, sessionService) {
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
                    CordovaService.prototype.login = function (user, password) {
                        var _this = this;
                        return this.$http({
                            method: 'POST',
                            url: "https://aaatest.wolterskluwercloud.com/auth/core/connect/token",
                            data: this.generatePostData(user, password),
                            headers: {
                                'Content-Type': 'application/x-www-form-urlencoded',
                                'Authorization': this.generateAuthorizationHeader()
                            }
                        }).then(function (response) {
                            if (response && response.data) {
                                var result;
                                result = response.data;
                                _this.sessionService.token = result.access_token;
                                return result.access_token;
                            }
                            return "";
                        }, function (e) {
                            return _this.$q.reject(e);
                        });
                    };
                    CordovaService.prototype.generatePostData = function (user, password) {
                        var userName = encodeURIComponent(user);
                        return "grant_type=password&username=" + userName + "&password=" + password + "&scope=WK.UK.PracticePortal.Access";
                    };
                    CordovaService.prototype.generateAuthorizationHeader = function () {
                        return 'Basic ' + this.encodeCredentials('WK.UK.PracticePortal.Mobile', '123456');
                    };
                    CordovaService.prototype.encodeCredentials = function (name, password) {
                        var credential = name + ':' + password;
                        return this.base64.encode(credential);
                    };
                    CordovaService.prototype.loadConversations = function (skip, take, displayMessage, filter) {
                        var _this = this;
                        return this.$http({
                            method: 'GET',
                            url: 'http://cchppdev.azurewebsites.net/api/PracticePortal/conversations',
                            params: {
                                take: take,
                                skip: skip,
                                previewLimit: displayMessage,
                                filter: filter
                            },
                            headers: {
                                'Accept': "text/html",
                                'Content-Type': "application/json",
                            }
                        }).then(function (response) {
                            if (response && response.data)
                                return response.data;
                            return [];
                        }, function (e) {
                            return _this.$q.reject(e);
                        });
                    };
                    CordovaService.prototype.postImage = function (imageString) {
                        return this.$http.post("http://cchppdev.azurewebsites.net/api/api/practiceportal/documents", { imageString: imageString });
                    };
                    return CordovaService;
                }());
                module.factory("$cordova", ["$q", "$injector", "$rootScope", "$http", 'oBase64', 'sessionService', function ($q, $injector, $rootScope, $http, base64, sessionService) { return new CordovaService($q, $injector, $rootScope, $http, base64, sessionService); }]);
            })(Services = Core.Services || (Core.Services = {}));
        })(Core = Mobile.Core || (Mobile.Core = {}));
    })(Mobile = PracticePortal.Mobile || (PracticePortal.Mobile = {}));
})(PracticePortal || (PracticePortal = {}));
var PracticePortal;
(function (PracticePortal) {
    var Mobile;
    (function (Mobile) {
        var Core;
        (function (Core) {
            var Services;
            (function (Services) {
                var SessionService;
                (function (SessionService_1) {
                    "use strict";
                    var module = angular.module("PracticePortal.Mobile.Core.Services.SessionService", []);
                    var SessionService = (function () {
                        function SessionService() {
                            this.token = "";
                        }
                        SessionService.prototype.getAccessToken = function () {
                            return this.token;
                        };
                        SessionService.prototype.clearAccessToken = function () {
                            this.token = "";
                        };
                        return SessionService;
                    }());
                    module.service('sessionService', function () { return new SessionService(); });
                })(SessionService = Services.SessionService || (Services.SessionService = {}));
            })(Services = Core.Services || (Core.Services = {}));
        })(Core = Mobile.Core || (Mobile.Core = {}));
    })(Mobile = PracticePortal.Mobile || (PracticePortal.Mobile = {}));
})(PracticePortal || (PracticePortal = {}));
//# sourceMappingURL=app.js.map