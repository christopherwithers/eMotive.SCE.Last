var formManagementApp = angular.module('formManagementApp', ['ngRoute', 'formServices', 'mgcrea.ngStrap']);

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

formManagementApp.controller("HomeController", function ($scope, $formServices) {

 //   alert($scope.signup.AcademicYear);

});