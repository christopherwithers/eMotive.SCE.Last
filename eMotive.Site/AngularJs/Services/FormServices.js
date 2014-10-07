angular.module('formServices', []).factory('$formServices', function ($http) {
    return {
        getAllFormLists: function () {
            return $http.get(siteRoot + "api/Forms/FormList/").then(function (result) {
                return result.data;
            });
        },
        getFormLists: function (ids) {
            return $http.get(siteRoot + "api/Forms/FormList/", { params: { Ids: ids } }).then(function (result) {
                return result.data;
            });
        },
        newFormList: function () {
            return $http.get(siteRoot + "api/Forms/FormList/New").then(function (result) {
                return result.data;
            });
        },
        createFormList: function (data) {
            return $http.post(siteRoot + "api/Forms/FormList/", { FormList: data }).then(function (result) {
                return result.data;
            });
        },
        updateFormList: function (data) {
            return $http.put(siteRoot + "api/Forms/FormList/", { FormList: data }).then(function (result) {
                return result.data;
            });
        },
        deleteFormList: function (id) {

            return $http.delete(siteRoot + "api/Forms/FormList/", { params: { Id: id } }).then(function (result) {
                return result.data;
            });
        }
    };
});