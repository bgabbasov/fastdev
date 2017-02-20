(function () {
  'use strict';

  angular.module("aApp", [])

      .factory("aFactory", ["$http", function ($http) {
        return {
          getA: function (pageNo, pageSize) {
            return $http.get("/api/a?page=" + pageNo + "&pagesize=" + pageSize);
          }
        };
      }])

      .controller("aListController", ["$scope", "aFactory", function ($scope, aFactory) {
        $scope.totalPages = 1;
        $scope.page = 1;
        $scope.image = null;
        $scope.aList = [];

        $scope.showImage = function (id, fileNo) {
          $scope.image = "/api/a/" + id + "/" + fileNo;
        };

        $scope.load = function () {
          aFactory.getA($scope.page, 10).success(function (data) {
            $scope.aList = data.data;
            $scope.totalPages = data.totalPages;
          }).error(function (error) {
            alert(JSON.stringify(error));
          });
        };

        $scope.load();
      }]);

})();