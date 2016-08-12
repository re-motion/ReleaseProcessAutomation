﻿. $PSScriptRoot"\..\Core\main_functions.ps1"
. $PSScriptRoot"\Test_Functions.ps1"
. $PSScriptRoot"\..\Core\config_functions.ps1"
. $PSScriptRoot"\..\Core\jira_functions.ps1"
. $PSScriptRoot"\..\Core\main_helper_functions.ps1"
. $PSScriptRoot"\..\Core\read_functions.ps1"
. $PSScriptRoot"\..\Core\semver_functions.ps1"


#There was an Issue that $PSScriptRoot was null in BeforeEach/AfterEach, so we have to cache it here
$ScriptRoot = $PSScriptRoot

$TestBaseDir = "C:\temp"
$TestDir = "$($TestBaseDir)\ReleaseProcessScriptTestRepository"
$TestDirName = "GitUnitTestDir"
$PseudoRemoteTestDir = "RemoteTestDir"

Describe "IntegrationTests" {
  
  BeforeEach {
    #Mock Things which we dont want to test automated in an integration test as they could break something online
    Mock Push-To-Repos { return }
    Test-Mock-All-Jira-Functions

    #Assure that the TestDir does not already exist
    Test-Path $TestDir | Should Be False
  }

  AfterEach {
    cd $ScriptRoot
    Remove-Item $TestDir -Recurse -Force  
  }

  Context "ReleasePatchOnMaster" {
    It "ReleasePatchOnMaster" {
        
      Copy-Item -Destination $TestDir -Path ".\TestDirectories\BaseDirectory" -Recurse
      cd $TestDir    
        
      Mock Read-Version-Choice { return "1.0.2" }
      git checkout master --quiet
      
      { Release-Version } | Should Not Throw
        
      git remote add -f MasterRepo "$($ScriptRoot)\TestDirectories\ReleasePatchOnMaster"
      git remote update

      git checkout master --quiet

      #Compare File Structure
      git diff master remotes/MasterRepo/master | Should BeNullOrEmpty 
      git diff release/v1.0.1 master remotes/MasterRepo/release/v1.0.1 | Should BeNullOrEmpty 
        
      #Compare commit Trees
      $LocalMasterLog = git log master --graph --pretty=format:'%d %s'
      $LocalReleaseLog = git log release/v1.0.1 --graph --pretty=format:'%d %s'

      cd "$($ScriptRoot)\TestDirectories\ReleasePatchOnMaster"
      git checkout master --quiet
      $RemoteMasterLog = git log master --graph --pretty=format:'%d %s'
      $RemoteReleaseLog = git log release/v1.0.1 --graph --pretty=format:'%d %s'
      
      $LocalMasterLog | Should Be $RemoteMasterLog
      $LocalReleaseLog | Should Be $RemoteReleaseLog
    }
  }
  
  Context "ReleaseVersionOnMaster" {
    It "ReleaseVersionOnMaster" {

      Copy-Item -Destination $TestDir -Path ".\TestDirectories\BaseDirectory" -Recurse
      cd $TestDir

      git checkout master --quiet
      git checkout -b develop --quiet
      
      Mock Get-Develop-Current-Version { return "1.1.0" }
      Mock Read-Version-Choice { return "1.2.0" }
      
      { Release-Version } | Should Not Throw
      git remote add -f MasterRepo "$($ScriptRoot)\TestDirectories\ReleaseVersionOnMaster"
      git remote update

      git checkout master --quiet

      #Compare File Structure
      git diff master remotes/MasterRepo/master | Should BeNullOrEmpty 
      git diff develop remotes/MasterRepo/develop | Should BeNullOrEmpty 
      git diff release/v1.1.0 master remotes/MasterRepo/release/v1.1.0 | Should BeNullOrEmpty 
        
      #Compare commit Trees
      $LocalMasterLog = git log master --graph --pretty=format:'%d %s'
      $LocalDevelopLog = git log develop --graph --pretty=format:'%d %s'
      $LocalReleaseLog = git log release/v1.1.0 --graph --pretty=format:'%d %s'

      cd "$($ScriptRoot)\TestDirectories\ReleaseVersionOnMaster"
      git checkout master --quiet
      $RemoteMasterLog = git log master --graph --pretty=format:'%d %s'
      $RemoteDevelopLog = git log develop --graph --pretty=format:'%d %s'
      $RemoteReleaseLog = git log release/v1.1.0 --graph --pretty=format:'%d %s'
      
      $LocalMasterLog | Should Be $RemoteMasterLog
      $LocalDevelopLog | Should Be $RemoteDevelopLog
      $LocalReleaseLog | Should Be $RemoteReleaseLog
    }
  }  
}