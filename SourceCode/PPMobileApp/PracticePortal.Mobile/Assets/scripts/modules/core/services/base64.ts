﻿/// <reference path="../../../../../typings/tsd.d.ts" />

module PracticePortal.Mobile.Core.Services.Base64  {
    "use strict";

    var module = angular.module("PracticePortal.Mobile.Core.Services.Base64", []);

    export interface IBase64 {
        /**
         * encodes a  string to base64
         * @text: the string to encode;
         * @returns: the resulting base 64 string
         */
        encode(text: string): string;

        /**
         * decodes a base64 string
         * @b64String: the base 64 string to decode
         * @returns:  the decoded text
         */
        decode(b64String: string): string;
    }

    class Base64 implements IBase64 {
        // private property
        private _keyStr;

        constructor() {
            this._keyStr = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";
        }
        // public method for encoding
        public encode(input: string) {
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
                } else if (isNaN(chr3)) {
                    enc4 = 64;
                }

                output = output +
                    this._keyStr.charAt(enc1) + this._keyStr.charAt(enc2) +
                    this._keyStr.charAt(enc3) + this._keyStr.charAt(enc4);

            }

            return output;
        }

        // public method for decoding

        public decode(input: string) {
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

        }

        // private method for UTF-8 encoding
        private _utf8_encode($string: string) {
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
        }

        // private method for UTF-8 decoding
        private _utf8_decode(utftext: string) {
            var $string = "";
            var i = 0;
            var c = 0;
            var c1 = 0;
            var c2 = 0

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
        }
    }

    module.service('oBase64', () => new Base64());
}


