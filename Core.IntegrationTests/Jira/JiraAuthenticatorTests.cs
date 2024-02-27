using NUnit.Framework;
using Remotion.ReleaseProcessAutomation.Jira.Authentication;
using Remotion.ReleaseProcessAutomation.Jira.CredentialManagement;

namespace Remotion.ReleaseProcessAutomation.IntegrationTests.Jira;

[TestFixture]
[Explicit]
public class JiraAuthenticatorTests
{
  private const string c_jiraUrl = "https://re-motion.atlassian.net/";
  private const string c_jiraProjectKey = "SRCBLDTEST";

  [Test]
  public void CheckAuthentication_WithCorrectCredentials_DoesNotThrow ()
  {
    var testCredentials = JiraTestUtility.GetLocallySavedCredentials();

    var authenticator = new JiraAuthenticator();

    Assert.That(() => authenticator.CheckAuthentication(testCredentials, c_jiraProjectKey, c_jiraUrl), Throws.Nothing);
  }

  [Test]
  public void CheckAuthentication_WithIncorrectCredentials_DoesThrow ()
  {
    var testCredentials = new Credentials { Username = "DefinetlyNotAUsername", Password = "DefinetlyNotAPassword" };

    var authenticator = new JiraAuthenticator();

    Assert.That(() => authenticator.CheckAuthentication(testCredentials, c_jiraProjectKey, c_jiraUrl), Throws.Exception);
  }
}