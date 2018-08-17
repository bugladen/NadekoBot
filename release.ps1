function GitHub-Release($versionNumber) {
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
    $ErrorActionPreference = "Stop"

    git pull
    git push #making sure commit id exists on remote

    $nl = [Environment]::NewLine
    $env:NADEKOBOT_INSTALL_VERSION = $versionNumber
    $gitHubApiKey = $env:GITHUB_API_KEY
    $commitId = git rev-parse HEAD
    $lastTag = git describe --tags --abbrev=0
    $tag = "$lastTag..HEAD"

    $clArr = (& 'git' 'log', $tag, '--oneline')
    [array]::Reverse($clArr)
    $changelog = $clArr | where { "$_" -notlike "*(POEditor.com)*" -and "$_" -notlike "*Merge branch*" -and "$_" -notlike "*Merge pull request*" -and "$_" -notlike "^-*" -and "$_" -notlike "*Merge remote tracking*" }
    $changelog = [string]::join([Environment]::NewLine, $changelog)

    $cl2 = $clArr | where { "$_" -like "*Merge pull request*" }
    $changelog = "## Changes$nl$changelog"
    if ($cl2 -ne $null) {
        $cl2 = [string]::join([Environment]::NewLine, $cl2)
        $changelog = $changelog + "$nl ## Pull Requests Merged$nl$cl2"
    }

    Write-Host $changelog 

    dotnet publish -c Release --runtime win7-x64

    # set-alias sz "$env:ProgramFiles\7-Zip\7z.exe" 
    # $source = "src\NadekoBot\bin\Release\PublishOutput\win7-x64" 
    # $target = "src\NadekoBot\bin\Release\PublishOutput\NadekoBot.7z"

    # sz 'a' '-mx3' $target $source

    .\rcedit-x64.exe "src\NadekoBot\bin\Release\netcoreapp2.1\win7-x64\nadekobot.exe" --set-icon "src\NadekoBot\bin\Release\netcoreapp2.1\win7-x64\nadeko_icon.ico"

    & "C:\Program Files (x86)\Inno Setup 5\iscc.exe" "/O+" ".\NadekoBot.iss"

    $artifact = "NadekoBot-setup-$versionNumber.exe";

    $auth = 'Basic ' + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($gitHubApiKey + ":x-oauth-basic"));
    Write-Host $changelog
    $result = GitHubMake-Release $versionNumber $commitId $TRUE $gitHubApiKey $auth "" "$changelog"
    $releaseId = $result | Select -ExpandProperty id
    $uploadUri = $result | Select -ExpandProperty upload_url
    $uploadUri = $uploadUri -creplace '\{\?name,label\}', "?name=$artifact"
    Write-Host $releaseId $uploadUri
    $uploadFile = [Environment]::GetFolderPath('MyDocuments') + "\projekti\NadekoInstallerOutput\$artifact"

    $uploadParams = @{
        Uri         = $uploadUri;
        Method      = 'POST';
        Headers     = @{
            Authorization = $auth;
        }
        ContentType = 'application/x-msdownload';
        InFile      = $uploadFile
    }

    Write-Host 'Uploading artifact'
    $result = Invoke-RestMethod @uploadParams
    Write-Host 'Artifact upload finished.'
    $result = GitHubMake-Release $versionNumber $commitId $FALSE $gitHubApiKey $auth "$releaseId"
    git pull
    Write-Host 'Done 🎉'
}

function GitHubMake-Release($versionNumber, $commitId, $draft, $gitHubApiKey, $auth, $releaseId, $body) {
    $releaseId = If ($releaseId -eq "") {""} Else {"/" + $releaseId};

    Write-Host $versionNumber
    Write-Host $commitId
    Write-Host $draft
    Write-Host $releaseId
    Write-Host $body

    $releaseData = @{
        tag_name         = $versionNumber;
        target_commitish = $commitId;
        name             = [string]::Format("NadekoBot v{0}", $versionNumber);
        body             = $body;
        draft            = $draft;
        prerelease       = $releaseId -ne "";
    }

    $releaseParams = @{
        Uri         = "https://api.github.com/repos/Kwoth/NadekoBot/releases" + $releaseId;
        Method      = 'POST';
        Headers     = @{
            Authorization = $auth;
        }
        ContentType = 'application/json';
        Body        = (ConvertTo-Json $releaseData -Compress)
    }
    return Invoke-RestMethod @releaseParams
}