﻿using SCQueryConnect.Interfaces;
using System;
using System.Linq;

namespace SCQueryConnect.Helpers
{
    public class ConnectionNameValidator : IConnectionNameValidator
    {
        private static readonly char[] InvalidChars =
        {
            '<',
            '>',
            ':',
            '"',
            '/',
            '\\',
            '|',
            '?',
            '*'
        };

        private static readonly string[] InvalidNames =
        {
            "CON",
            "PRN",
            "AUX",
            "NUL",
            "COM1",
            "COM2",
            "COM3",
            "COM4",
            "COM5",
            "COM6",
            "COM7",
            "COM8",
            "COM9",
            "LPT1",
            "LPT2",
            "LPT3",
            "LPT4",
            "LPT5",
            "LPT6",
            "LPT7",
            "LPT8",
            "LPT9"
        };

        private readonly Func<string, string>[] _validationChecks = {
            CheckForInvalidCharacters,
            CheckForInvalidNames,
            CheckNameDoesNotEndWithDot
        };

        public string Validate(string connectionName)
        {
            var message = string.Empty;

            foreach (var check in _validationChecks)
            {
                message = check(connectionName);

                var isInvalid = !string.IsNullOrWhiteSpace(message);
                if (isInvalid)
                {
                    break;
                }
            }

            return message;
        }

        private static string CheckForInvalidCharacters(string connectionName)
        {
            var hasInvalidChar = InvalidChars.Any(connectionName.Contains);

            if (hasInvalidChar)
            {
                var invalidChar = InvalidChars.First(connectionName.Contains);
                return $"Connection names cannot contain '{invalidChar}'";
            }

            return string.Empty;
        }

        private static string CheckForInvalidNames(string connectionName)
        {
            var invalidName = InvalidNames.FirstOrDefault(n =>
                string.Compare(n, connectionName, StringComparison.OrdinalIgnoreCase) == 0);

            var isInvalidName = !string.IsNullOrWhiteSpace(invalidName);

            if (isInvalidName)
            {
                return $"The Name '{invalidName}' is not valid";
            }

            return string.Empty;
        }

        private static string CheckNameDoesNotEndWithDot(string connectionName)
        {
            var message = connectionName.EndsWith(".")
                ? "Names cannot end with '.'"
                : string.Empty;

            return message;
        }
    }
}