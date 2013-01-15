﻿describe("when validator for property becomes invalid", function () {
    var isValid = ko.observable(true);
    var instance = null;

    var parameters = {
        commandCoordinator: {
        },
        commandValidationService: {
            applyRulesToProperties: function (command) {

                command.something.extend({ validation: {} });

                return [{
                    isValid: isValid
                }];
            }
        }
    };

    beforeEach(function () {
        ko.extenders.validation = function (target, options) {
            return target;
        };

        var commandType = Bifrost.commands.Command.extend(function () {
            var self = this;
            this.something = ko.observable(1);
        });

        instance = commandType.create(parameters);
        isValid(false);
    });

    afterEach(function () {
        ko.extenders.validation = undefined;
    });

    it("should not be able to execute", function () {
        expect(instance.canExecute()).toBe(false);
    });

    it("should not be valid", function () {
        expect(instance.isValid()).toBe(false);
    });
});