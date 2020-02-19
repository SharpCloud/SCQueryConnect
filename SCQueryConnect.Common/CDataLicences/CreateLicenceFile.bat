@echo off

if exist CDataLicences.txt goto end
echo Existing licence file not found. Creating template licence file...
echo Access=1234567890123456789012345678901234567890>"CDataLicences.txt"
echo Excel=1234567890123456789012345678901234567890>>"CDataLicences.txt"
echo SharePoint=1234567890123456789012345678901234567890>>"CDataLicences.txt"
echo Complete!

:end
