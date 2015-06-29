/// <reference path="../typings/angularjs/angular.d.ts"/>
var App;
(function (App) {
    App.app;
    App.app = angular.module('NumberLookup', ["SignalR"]);
    App.app.controller("HomeController", ["$scope", "$http", "NumberLookupService", function ($scope, $http, lookupService) {
            $scope.LookupsPerSecond = 123;
            $scope.running = false;
            $scope.submit = function () {
                $scope.running = true;
                lookupService.start($scope.LookupsPerSecond);
                $scope.rates = lookupService.rates;
            };
            $scope.stop = function () {
                $scope.running = false;
                lookupService.stop();
            };
            $scope.getRates = function () {
                return lookupService.getRates();
            };
            $scope.setRate = function () {
                if ($scope.running) {
                    lookupService.setRate($scope.LookupsPerSecond);
                }
                else {
                    $scope.submit();
                }
            };
            $scope.setDnsRate = function () {
                $http
                    .post("http://localhost:9000/api/RateLimit", $scope.DnsRequestsPerSecond);
            };
        }]);
    App.app.factory("NumberLookupService", [
        "$rootScope", "Hub", "$timeout", function ($rootScope, Hub, $timeout) {
            var rates = [];
            var hub = new Hub("NumberLookup", {
                listeners: {
                    'hello': function (result) {
                        rates.push({ Date: new Date(), Count: result });
                        $rootScope.$apply();
                    }
                },
                methods: ["start", "stop", "setRate"],
                errorHandler: function (error) {
                    console.error(error);
                },
                stateChanged: function (state) {
                    switch (state.newState) {
                        case window["$"].signalR.connectionState.connecting:
                            break;
                        case window["$"].signalR.connectionState.connected:
                            break;
                        case window["$"].signalR.connectionState.reconnecting:
                            break;
                        case window["$"].signalR.connectionState.disconnected:
                            break;
                    }
                }
            });
            var start = function (str) {
                rates = [];
                hub.start(str);
            };
            var stop = function () {
                hub.stop();
            };
            var setRate = function (rate) {
                hub.setRate(rate);
            };
            var getRates = function () {
                return rates;
            };
            return {
                start: start,
                stop: stop,
                setRate: setRate,
                getRates: getRates
            };
        }
    ]);
    App.app.directive("ngEnter", function () { return function (scope, element, attrs) {
        element.bind("keydown keypress", function (event) {
            if (event.which === 13) {
                scope.$apply(function () {
                    scope.$eval(attrs.ngEnter);
                });
                event.preventDefault();
            }
        });
    }; });
})(App || (App = {}));
//# sourceMappingURL=app.js.map