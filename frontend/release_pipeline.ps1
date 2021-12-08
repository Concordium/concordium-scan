param(
    [string]$GSA_KEY
    [string]$fireBaseProject,
)
$dir = Split-Path $MyInvocation.MyCommand.Path
Push-Location $dir

SetEnvironmentVariable('GOOGLE_APPLICATION_CREDENTIALS',$GSA_KEY)


npm i -g firebase-tools
write-host "starting deploy...";
firebase --version;
firebase deploy --project $fireBaseProject --message "Release: $releaseMessage";
write-host "deployment completed";

Pop-Location