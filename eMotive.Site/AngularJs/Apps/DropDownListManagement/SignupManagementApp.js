var signupManagementApp = angular.module('signupManagementApp', ['ngRoute', 'signupServices', 'mgcrea.ngStrap']);

signupManagementApp.config(function ($datepickerProvider) {
    var dir = siteRoot + "/AngularJs/Apps/DropDownListManagement/Templates/";

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

signupManagementApp.controller("myApp", function ($scope, $signupServices, $location) {
    var url = $location.absUrl();

   var id = url.substring(url.lastIndexOf("/") + 1);

   // $scope.groups = $signupServices.getAllGroups();

 /*   $signupServices.getGroup().then(function(data) {
        if (data.Success) {
            $scope.groups = data.Result;
            // alert($scope.signup.AcademicYear);
        } else {
            alert("error!");

        }*/
    });

    $signupServices.getSignup(url.substring(url.lastIndexOf("/") + 1)).then(function(data) {

        if (data.Success) {
            $scope.signup = data.Result[0];
           // alert($scope.signup.AcademicYear);
        } else {
            alert("error!");

        }
    });

    $scope.saveSignup = function() {
        $signupServices.saveSignup($scope.signup).then(function(data) {
            if (!data.Success) {
                alert(data.Errors[0]);
            }
        });
    }

 //   alert($scope.signup.AcademicYear);

});