using System;
using System.Net;
using AdysTech.CredentialManager;
using Moq;
using NUnit.Framework;
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Jira.Authentication;
using ReleaseProcessAutomation.Jira.CredentialManagement;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.ReadInput;
using Spectre.Console.Testing;

namespace ReleaseProcessAutomation.UnitTests.Jira;

[TestFixture]
public class JiraCredentialManagerTests
{
  private const string c_userName = "user";
  private const string c_password = "password";
  private const string c_target = "target";

  private Mock<IInputReader> _inputReaderMock;
  private Mock<IJiraAuthenticator> _jiraAuthenticatorMock;
  private Mock<IJiraCredentialAPI> _jiraCredentialAPIMock;
  private TestConsole _console;
  private Configuration.Data.Config _config;

  [SetUp]
  public void Setup ()
  {
    _inputReaderMock = new Mock<IInputReader>();
    _jiraAuthenticatorMock = new Mock<IJiraAuthenticator>();
    _jiraCredentialAPIMock = new Mock<IJiraCredentialAPI>();
    _console = new TestConsole();
    _config = new Configuration.Data.Config();
    _config.Jira = new JiraConfig();
    _config.Jira.JiraURL = "https://www.JiraURL.com/url";
    _config.Jira.JiraProjectKey = "JiraProjectKey";
    _jiraAuthenticatorMock.Setup(
        _ => _.CheckAuthentication(
            It.IsAny<Credentials>(),
            _config.Jira.JiraProjectKey,
            _config.Jira.JiraURL));
  }

  [TearDown]
  public void TearDown ()
  {
    try
    {
      CredentialManager.RemoveCredentials(c_target);
    }
    catch
    {
      //ignore
    }
  }

  [Test]
  public void GetCredential_CredentialsInCredentialManager_ReturnsCredentials ()
  {
    var jiraCredentialManager = new JiraCredentialManager(_config, _inputReaderMock.Object, _console, _jiraAuthenticatorMock.Object, _jiraCredentialAPIMock.Object);

    _jiraCredentialAPIMock.Setup(_ => _.GetCredential(c_target)).Returns(new Credentials(c_userName, c_password));

    var output = jiraCredentialManager.GetCredential(c_target);

    Assert.That(output.Username, Is.EqualTo(c_userName));
    Assert.That(output.Password, Is.EqualTo(c_password));
  }

  [Test]
  public void GetCredential_NoCredentialsInCredentialManager_AsksUser ()
  {
    var jiraCredentialManager = new JiraCredentialManager(_config, _inputReaderMock.Object, _console, _jiraAuthenticatorMock.Object, _jiraCredentialAPIMock.Object);

    _inputReaderMock.Setup(_ => _.ReadString(It.IsAny<string>())).Returns(c_userName);
    _inputReaderMock.Setup(_ => _.ReadHiddenString(It.IsAny<string>())).Returns(c_password);
    _inputReaderMock.Setup(_ => _.ReadConfirmation(It.IsAny<bool>())).Returns(false);

    jiraCredentialManager.GetCredential(c_target);

    _inputReaderMock.Verify(_ => _.ReadString(It.IsAny<string>()));
    _inputReaderMock.Verify(_ => _.ReadHiddenString(It.IsAny<string>()));
    _inputReaderMock.Verify(_ => _.ReadConfirmation(It.IsAny<bool>()));
  }

  [Test]
  public void GetCredential_AsksUser_SavesPassword ()
  {
    var jiraCredentialManager = new JiraCredentialManager(_config, _inputReaderMock.Object, _console, _jiraAuthenticatorMock.Object, _jiraCredentialAPIMock.Object);

    _inputReaderMock.Setup(_ => _.ReadString(It.IsAny<string>())).Returns(c_userName);
    _inputReaderMock.Setup(_ => _.ReadHiddenString(It.IsAny<string>())).Returns(c_password);
    //wants to save pw
    _inputReaderMock.Setup(_ => _.ReadConfirmation(It.IsAny<bool>())).Returns(true);

    jiraCredentialManager.GetCredential(c_target);
    
    _jiraCredentialAPIMock.Verify(_ => _.SaveCredentials(It.IsAny<Credentials>(), c_target), Times.Once);
  }

  [Test]
  public void GetCredential_WithWrongCredentials_AsksUser ()
  {
    var username = "NotUserName";
    var password = "NotPassword";
    var networkCredential = new NetworkCredential(username, password);
    CredentialManager.SaveCredentials(c_target, networkCredential);

    var jiraCredentialManager = new JiraCredentialManager(_config, _inputReaderMock.Object, _console, _jiraAuthenticatorMock.Object, _jiraCredentialAPIMock.Object);

    _jiraAuthenticatorMock.Setup(
            _ => _.CheckAuthentication(
                It.IsAny<Credentials>(),
                _config.Jira.JiraProjectKey,
                _config.Jira.JiraURL))
        .Throws(new JiraException("error") { HttpStatusCode = HttpStatusCode.Unauthorized });

    _inputReaderMock.Setup(_ => _.ReadString(It.IsAny<string>())).Returns(c_userName);
    _inputReaderMock.Setup(_ => _.ReadHiddenString(It.IsAny<string>())).Returns(c_password);
    _inputReaderMock.Setup(_ => _.ReadConfirmation(It.IsAny<bool>())).Returns(false);

    Assert.That(
        () => jiraCredentialManager.GetCredential(c_target),
        Throws.InstanceOf<JiraAuthenticationException>()
            .With.Message.EqualTo("Authentication not successful, user does not want to try again."));

    _inputReaderMock.Verify(_ => _.ReadString(It.IsAny<string>()));
    _inputReaderMock.Verify(_ => _.ReadHiddenString(It.IsAny<string>()));
    _inputReaderMock.Verify(_ => _.ReadConfirmation(It.IsAny<bool>()));

    _jiraAuthenticatorMock.Verify(
        _ => _.CheckAuthentication(
            It.IsAny<Credentials>(),
            _config.Jira.JiraProjectKey,
            _config.Jira.JiraURL));
  }

  [Test]
  public void GetCredential_WithWrongCredentials_SavesNewCredentials ()
  {
    _jiraCredentialAPIMock.Setup(_ => _.GetCredential(c_target)).Returns(new Credentials("DefinetlyNotAUsername", "DefinetlyNotAPassword"));

    var jiraCredentialManager = new JiraCredentialManager(_config, _inputReaderMock.Object, _console, _jiraAuthenticatorMock.Object, _jiraCredentialAPIMock.Object);
    var sequence = new MockSequence();

    _jiraAuthenticatorMock.InSequence(sequence).Setup(
            _ => _.CheckAuthentication(
                It.IsAny<Credentials>(),
                _config.Jira.JiraProjectKey,
                _config.Jira.JiraURL))
        .Throws(new JiraException("error") { HttpStatusCode = HttpStatusCode.Unauthorized });

    _jiraAuthenticatorMock.InSequence(sequence)
        .Setup(_ => _.CheckAuthentication(It.IsAny<Credentials>(), _config.Jira.JiraProjectKey, _config.Jira.JiraURL));

    _inputReaderMock.Setup(_ => _.ReadString(It.IsAny<string>())).Returns(c_userName);
    _inputReaderMock.Setup(_ => _.ReadHiddenString(It.IsAny<string>())).Returns(c_password);
    _inputReaderMock.Setup(_ => _.ReadConfirmation(It.IsAny<bool>())).Returns(true);

    jiraCredentialManager.GetCredential(c_target);

    _jiraAuthenticatorMock.Verify(
        _ => _.CheckAuthentication(It.IsAny<Credentials>(), _config.Jira.JiraProjectKey, _config.Jira.JiraURL),
        Times.Exactly(2));
  }

  [Test]
  public void GetCredential_WithWrongCredentialInput_RepeatsFiveTimes ()
  {
    var jiraCredentialManager = new JiraCredentialManager(_config, _inputReaderMock.Object, _console, _jiraAuthenticatorMock.Object, _jiraCredentialAPIMock.Object);

    _jiraAuthenticatorMock.Setup(
            _ => _.CheckAuthentication(
                It.IsAny<Credentials>(),
                _config.Jira.JiraProjectKey,
                _config.Jira.JiraURL))
        .Throws(new JiraException("error") { HttpStatusCode = HttpStatusCode.Unauthorized });

    _inputReaderMock.Setup(_ => _.ReadString(It.IsAny<string>())).Returns(c_userName);
    _inputReaderMock.Setup(_ => _.ReadHiddenString(It.IsAny<string>())).Returns(c_password);

    var callCount = 5;
    _inputReaderMock.SetupSequence(_ => _.ReadConfirmation(It.IsAny<bool>()))
        .Returns(true)
        .Returns(true)
        .Returns(true)
        .Returns(true)
        .Returns(false);

    Assert.That(
        () => jiraCredentialManager.GetCredential(c_target),
        Throws.InstanceOf<JiraAuthenticationException>()
            .With.Message.EqualTo("Authentication not successful, user does not want to try again."));

    _jiraAuthenticatorMock.Verify(
        _ => _.CheckAuthentication(
            It.IsAny<Credentials>(),
            _config.Jira.JiraProjectKey,
            _config.Jira.JiraURL),
        Times.Exactly(callCount));
  }
}