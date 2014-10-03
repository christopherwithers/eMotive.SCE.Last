angular.module('formServices', []).factory('$formServices', function ($http) {
    return {
        getAllFormLists: function () {
            return $http.get("api/Forms/FormList/").then(function (result) {
                return result.data;
            });
        },
        getFormLists: function (ids) {
            return $http.get("api/Forms/FormList/", { params: { Ids: ids } }).then(function (result) {
                return result.data;
            });
        },
        newFormList: function () {
            return $http.get("api/Forms/FormList/New").then(function (result) {
                return result.data;
            });
        },
        createFormList: function (data) {
            return $http.post("api/Forms/FormList/", { FormList: data }).then(function (result) {
                return result.data;
            });
        },
        updateFormList: function (data) {
            return $http.put("api/Forms/FormList/", { FormList: data }).then(function (result) {
                return result.data;
            });
        },
        deleteFormList: function (id) {

            return $http.delete("api/Forms/FormList/", { params: { Id: id } }).then(function (result) {
                return result.data;
            });
        }
    };
});