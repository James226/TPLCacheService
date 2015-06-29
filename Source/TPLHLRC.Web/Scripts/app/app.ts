/// <reference path="../typings/angularjs/angular.d.ts"/>

module App {
    export var app: ng.IModule;

    app = angular.module('NumberLookup', ["SignalR"]);

    app.controller("HomeController", ["$scope", "$http", "NumberLookupService", ($scope, $http, lookupService) => {
        $scope.LookupsPerSecond = 123;
        $scope.running = false;
        $scope.submit = () => {
            $scope.running = true;
            lookupService.start($scope.LookupsPerSecond);
            $scope.rates = lookupService.rates;
        }

        $scope.stop = () => {
            $scope.running = false;
            lookupService.stop();
        }

        $scope.getRates = () => {
            return lookupService.getRates();
        }

        $scope.setRate = () => {
            if ($scope.running) {
                lookupService.setRate($scope.LookupsPerSecond);
            } else {
                $scope.submit();
            }
        }

        $scope.setDnsRate = () => {
            $http
                .post("http://localhost:9000/api/RateLimit", $scope.DnsRequestsPerSecond);

        }
    }]);

    app.factory("NumberLookupService", [
        "$rootScope", "Hub", "$timeout", ($rootScope, Hub, $timeout) => {

            var rates = [];

            var hub = new Hub("NumberLookup", {

                listeners: {
                    'hello': (result) => {
                        rates.push({ Date: new Date(), Count: result});
                        $rootScope.$apply();
                    }
                },

                methods: ["start", "stop", "setRate"],

                errorHandler(error) {
                    console.error(error);
                },

                stateChanged(state) {
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

            const start = str => {
                rates = [];
                hub.start(str);
            };

            const stop = () => {
                hub.stop();
            };

            const setRate = rate => {
                hub.setRate(rate);
            };

            const getRates = () => {
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

    app.directive("ngEnter", () => (scope, element, attrs) => {
        element.bind("keydown keypress", event => {
            if (event.which === 13) {
                scope.$apply(() => {
                    scope.$eval(attrs.ngEnter);
                });

                event.preventDefault();
            }
        });
    });
}