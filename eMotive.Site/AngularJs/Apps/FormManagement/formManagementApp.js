var formManagementApp = angular.module('formManagementApp', ['ngRoute', 'formServices', 'mgcrea.ngStrap', 'httpPreConfig']);

formManagementApp.config(function ($routeProvider) {
    var dir = siteRoot + "AngularJs/Apps/FormManagement/Templates/";

    $routeProvider.when('/Home', {
        controller: 'HomeController',
        templateUrl: dir + 'Home.html'
    }).
    when('/Create', {
        controller: 'CreateController',
        templateUrl: dir + 'Create.html'
    }).
    when('/Edit', {
        controller: 'EditController',
        templateUrl: dir + 'Edit.html'
    }).
    when('/Audit', {
        controller: 'AuditController',
        templateUrl: dir + 'Audit.html'
    }).
    otherwise({
        redirectTo: '/Home'
    });
});



formManagementApp.controller("HomeController", function ($scope, $formServices, httpPreConfig) {//look into this httpPreConfig! Can we push it to directive?

    $formServices.getAllFormLists().then(function (data) {
        $scope.dropDownLists = data.Result;
    });
});

formManagementApp.controller("CreateController", function ($scope, $formServices) {


    $formServices.newFormList().then(function(data) {
        $scope.dropDownList = data.Result;
    });


    $scope.saveDropDown = function () {
        $formServices.createFormList($scope.dropDownList).then(function(data) {
            if (data.Success) {
                $location.path('/Home').replace();
            } else {
                $scope.errors = data.Errors[0]; //todo: need to break the string of errors into a UL or similar?
            }
        });
    };


    $scope.addItem = function (text) {
        $scope.dropDownList.Collection.push({
            Value: 0,
            Text: ''
        });
    };

    $scope.removeItem = function (index) {
        $scope.dropDownList.Collection.splice(index, 1);
    };


});
