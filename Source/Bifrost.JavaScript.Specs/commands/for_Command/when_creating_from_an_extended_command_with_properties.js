﻿describe("when creating from an extended command with properties", function () {
    var commandAppliedTo = null;
    var command = null;

    var parameters = {
        commandCoordinator: {
        },
        commandValidationService: {
            applyRulesToProperties: function (command) {
                commandAppliedTo = command
            }
        }
    }

    var commandType = Bifrost.commands.Command.extend(function () {
        this.integer = 0;
        this.number = 0.1;
        this.string = "";
        this.arrayOfIntegers = [];

        this.onCreated = function () {
        };

        this.onDispose = function () {
        };
    });

    command = commandType.create(parameters);

    it("should make the integer property as an observable", function () {

        expect(ko.isObservable(command.integer)).toBe(true);
    });

    it("should make the number property as an observable", function () {
        expect(ko.isObservable(command.number)).toBe(true);
    });

    it("should make the string property as an observable", function () {
        expect(ko.isObservable(command.string)).toBe(true);
    });

    it("should make the array property as an observable", function () {
        expect(ko.isObservable(command.arrayOfIntegers)).toBe(true);
    });
});