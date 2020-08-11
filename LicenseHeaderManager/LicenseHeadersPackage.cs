#region copyright

// Copyright (c) rubicon IT GmbH

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 

#endregion

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Core;
using EnvDTE;
using EnvDTE80;
using LicenseHeaderManager.ButtonHandler;
using LicenseHeaderManager.Headers;
using LicenseHeaderManager.Interfaces;
using LicenseHeaderManager.MenuItemCommands.Common;
using LicenseHeaderManager.MenuItemCommands.EditorMenu;
using LicenseHeaderManager.MenuItemCommands.FolderMenu;
using LicenseHeaderManager.MenuItemCommands.ProjectItemMenu;
using LicenseHeaderManager.MenuItemCommands.ProjectMenu;
using LicenseHeaderManager.MenuItemCommands.SolutionMenu;
using LicenseHeaderManager.Options;
using LicenseHeaderManager.ResultObjects;
using LicenseHeaderManager.Utils;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using CreateDocumentResult = Core.CreateDocumentResult;
using Language = LicenseHeaderManager.Options.Language;
using LicenseHeader = LicenseHeaderManager.Headers.LicenseHeader;
using Task = System.Threading.Tasks.Task;

namespace LicenseHeaderManager
{
  #region package infrastructure

  /// <summary>
  ///   This is the class that implements the package exposed by this assembly.
  ///   The minimum requirement for a class to be considered a valid package for Visual Studio
  ///   is to implement the IVsPackage interface and register itself with the shell.
  ///   This package uses the helper classes defined inside the Managed Package Framework (MPF)
  ///   to do it: it derives from the Package class that provides the implementation of the
  ///   IVsPackage interface and uses the registration attributes defined in the framework to
  ///   register itself and its components with the shell.
  /// </summary>
  // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
  // a package.
  [PackageRegistration (UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
  // This attribute is used to register the informations needed to show the this package
  // in the Help/About dialog of Visual Studio.
  [InstalledProductRegistration ("#110", "#112", Version, IconResourceID = 400)]
  // This attribute is needed to let the shell know that this package exposes some menus.
  [ProvideMenuResource ("Menus.ctmenu", 1)]
  [ProvideOptionPage (typeof (OptionsPage), c_licenseHeaders, c_general, 0, 0, true)]
  [ProvideOptionPage (typeof (LanguagesPage), c_licenseHeaders, c_languages, 0, 0, true)]
  [ProvideOptionPage (typeof (DefaultLicenseHeaderPage), c_licenseHeaders, c_defaultLicenseHeader, 0, 0, true)]
  [ProvideProfile (typeof (OptionsPage), c_licenseHeaders, c_general, 0, 0, true)]
  [ProvideProfile (typeof (LanguagesPage), c_licenseHeaders, c_languages, 0, 0, true)]
  [ProvideProfile (typeof (DefaultLicenseHeaderPage), c_licenseHeaders, c_defaultLicenseHeader, 0, 0, true)]
  [ProvideAutoLoad (VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
  [Guid (GuidList.guidLicenseHeadersPkgString)]
  [ProvideMenuResource ("Menus.ctmenu", 1)]
  public sealed class LicenseHeadersPackage : AsyncPackage, ILicenseHeaderExtension
  {
    /// <summary>
    ///   Default constructor of the package.
    ///   Inside this method you can place any initialization code that does not require
    ///   any Visual Studio service because at this point the package object is created but
    ///   not sited yet inside Visual Studio environment. The place to do all the other
    ///   initialization is the Initialize method.
    /// </summary>
    public LicenseHeadersPackage ()
    {
    }

    public const string Version = "3.0.3";

    private const string c_licenseHeaders = "License Header Manager";
    private const string c_general = "General";
    private const string c_languages = "Languages";
    private const string c_defaultLicenseHeader = "Default Header";

    private ProjectItemsEvents _projectItemEvents;
    private ProjectItemsEvents _websiteItemEvents;
    private CommandEvents _commandEvents;

    // TODO make private again and temporarily inject in command initialization until core lib is finished
    public DTE2 _dte;
    public AddLicenseHeaderToAllProjectsDelegate _addLicenseHeaderToAllProjectsDelegate;

    /// <summary>
    ///   Initialization of the package; this method is called right after the package is sited, so this is the
    ///   place where you can put all the initilaization code that rely on services provided by VisualStudio.
    /// </summary>
    protected override async Task InitializeAsync (CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
      await base.InitializeAsync (cancellationToken, progress);
      await JoinableTaskFactory.SwitchToMainThreadAsync (cancellationToken);

      OutputWindowHandler.Initialize (GetGlobalService (typeof (SVsOutputWindow)) as IVsOutputWindow);
      _dte = await GetServiceAsync (typeof (DTE)) as DTE2;
      Assumes.Present (_dte);
      _addedItems = new Stack<ProjectItem>();
      var buttonHandlerFactory = new ButtonHandlerFactory (this, LicenseHeaderReplacer);
      _addLicenseHeaderToAllProjectsDelegate = buttonHandlerFactory.CreateAddLicenseHeaderToAllProjectsButtonHandler();

      await AddHeaderToProjectItemCommand.InitializeAsync (this);
      await RemoveHeaderFromProjectItemCommand.InitializeAsync (this);
      await AddLicenseHeaderToAllFilesInSolutionCommand.InitializeAsync (this);
      await RemoveLicenseHeaderFromAllFilesInSolutionCommand.InitializeAsync (this);
      await AddNewSolutionLicenseHeaderDefinitionFileCommand.InitializeAsync (
          this,
          _dte?.Solution,
          () => ((DefaultLicenseHeaderPage) GetDialogPage (typeof (DefaultLicenseHeaderPage))).LicenseHeaderFileText);
      await OpenSolutionLicenseHeaderDefinitionFileCommand.InitializeAsync (this);
      await RemoveSolutionLicenseHeaderDefinitionFileCommand.InitializeAsync (this);
      await AddLicenseHeaderToAllFilesInProjectCommand.InitializeAsync (this);
      await RemoveLicenseHeaderFromAllFilesInProjectCommand.InitializeAsync (this);
      await AddNewLicenseHeaderDefinitionFileToProjectCommand.InitializeAsync (this);
      await AddExistingLicenseHeaderDefinitionFileToProjectCommand.InitializeAsync (this);
      await LicenseHeaderOptionsCommand.InitializeAsync (this);
      await AddLicenseHeaderToAllFilesInFolderCommand.InitializeAsync (this);
      await RemoveLicenseHeaderFromAllFilesInFolderCommand.InitializeAsync (this);
      await AddExistingLicenseHeaderDefinitionFileToFolderCommand.InitializeAsync (this);
      await AddNewLicenseHeaderDefinitionFileToFolderCommand.InitializeAsync (this);
      await AddLicenseHeaderEditorAdvancedMenuCommand.InitializeAsync (this);
      await RemoveLicenseHeaderEditorAdvancedMenuCommand.InitializeAsync (this);


      //register ItemAdded event handler
      var events = _dte.Events as Events2;
      if (events != null)
      {
        _projectItemEvents = events.ProjectItemsEvents; //we need to keep a reference, otherwise the object is garbage collected and the event won't be fired
        _projectItemEvents.ItemAdded += ItemAdded;

        //Register to WebsiteItemEvents for Website Projects to work
        //Reference: https://social.msdn.microsoft.com/Forums/en-US/dde7d858-2440-43f9-bbdc-3e1b815d4d1e/itemadded-itemremoved-and-itemrenamed-events-not-firing-in-web-projects?forum=vsx
        //Concerns, that the ItemAdded Event gets called on unrelated events, like closing the solution or opening folder, could not be reproduced
        try
        {
          _websiteItemEvents = events.GetObject ("WebSiteItemsEvents") as ProjectItemsEvents;
        }
        catch (Exception)
        {
          //TODO: Add log statement as soon as we have added logging.
          //This probably only throws an exception if no WebSite component is installed on the machine.
          //If no WebSite component is installed, they are probably not using a WebSite Project and therefore dont need that feature.
        }

        if (_websiteItemEvents != null)
          _websiteItemEvents.ItemAdded += ItemAdded;
      }

      //register event handlers for linked commands
      var page = (OptionsPage) GetDialogPage (typeof (OptionsPage));
      if (page != null)
      {
        foreach (var command in page.LinkedCommands)
        {
          command.Events = _dte.Events.CommandEvents[command.Guid, command.Id];

          switch (command.ExecutionTime)
          {
            case ExecutionTime.Before:
              command.Events.BeforeExecute += BeforeLinkedCommandExecuted;
              break;
            case ExecutionTime.After:
              command.Events.AfterExecute += AfterLinkedCommandExecuted;
              break;
          }
        }

        page.LinkedCommandsChanged += CommandsChanged;

        //register global event handler for ItemAdded
        _commandEvents = _dte.Events.CommandEvents;
        _commandEvents.BeforeExecute += BeforeAnyCommandExecuted;
      }
    }

    public bool SolutionHeaderDefinitionExists ()
    {
      var solutionHeaderDefinitionFilePath = LicenseHeader.GetHeaderDefinitionFilePathForSolution (_dte.Solution);
      return File.Exists (solutionHeaderDefinitionFilePath);
    }

    public bool ShouldBeVisible (ProjectItem item)
    {
      var visible = false;

      if (ProjectItemInspection.IsPhysicalFile (item))
        visible = LicenseHeaderReplacer.TryCreateDocument (item.FileNames[1], out _) == CreateDocumentResult.DocumentCreated;

      return visible;
    }

    public ProjectItem GetActiveProjectItem ()
    {
      try
      {
        var activeDocument = _dte.ActiveDocument;
        if (activeDocument == null)
          return null;
        return activeDocument.ProjectItem;
      }
      catch (ArgumentException)
      {
        return null;
      }
    }

    public bool _isCalledByLinkedCommand;

    public object GetSolutionExplorerItem ()
    {
      IntPtr hierarchyPtr, selectionContainerPtr;
      uint projectItemId;

      IVsMultiItemSelect mis;
      var monitorSelection = (IVsMonitorSelection) GetGlobalService (typeof (SVsShellMonitorSelection));

      monitorSelection.GetCurrentSelection (out hierarchyPtr, out projectItemId, out mis, out selectionContainerPtr);
      var hierarchy = Marshal.GetTypedObjectForIUnknown (hierarchyPtr, typeof (IVsHierarchy)) as IVsHierarchy;

      if (hierarchy != null)
      {
        object item;
        hierarchy.GetProperty (projectItemId, (int) __VSHPROPID.VSHPROPID_ExtObject, out item);
        return item;
      }

      return null;
    }

    /// <summary>
    ///   Executes a command asynchronously.
    /// </summary>
    private void PostExecCommand (Guid guid, uint id, object argument)
    {
      var shell = (IVsUIShell) GetService (typeof (SVsUIShell));
      shell.PostExecCommand (
          ref guid,
          id,
          (uint) vsCommandExecOption.vsCommandExecOptionDoDefault,
          ref argument);
    }

    #endregion

    public LicenseHeaderReplacer LicenseHeaderReplacer
    {
      get
      {
        var keywords = OptionsPage.UseRequiredKeywords
            ? OptionsPage.RequiredKeywords.Split (new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select (k => k.Trim())
            : null;
        return new LicenseHeaderReplacer (LanguagesPage.Languages.Select (Language.ToCoreLanguage), keywords);
      }
    }

    public void ShowLanguagesPage ()
    {
      ShowOptionPage (typeof (LanguagesPage));
    }

    public IDefaultLicenseHeaderPage DefaultLicenseHeaderPage => (DefaultLicenseHeaderPage) GetDialogPage (typeof (DefaultLicenseHeaderPage));

    public ILanguagesPage LanguagesPage => (LanguagesPage) GetDialogPage (typeof (LanguagesPage));

    public IOptionsPage OptionsPage => (OptionsPage) GetDialogPage (typeof (OptionsPage));

    public DTE2 Dte2 => _dte;

    #region event handlers

    private void BeforeLinkedCommandExecuted (string guid, int id, object customIn, object customOut, ref bool cancelDefault)
    {
      InvokeAddLicenseHeaderCommandFromLinkedCmd();
    }

    private void AfterLinkedCommandExecuted (string guid, int id, object customIn, object customOut)
    {
      InvokeAddLicenseHeaderCommandFromLinkedCmd();
    }

    private void InvokeAddLicenseHeaderCommandFromLinkedCmd ()
    {
      _isCalledByLinkedCommand = true;
      AddLicenseHeaderEditorAdvancedMenuCommand.Instance.Invoke();
      _isCalledByLinkedCommand = false;
    }

    private void CommandsChanged (object sender, NotifyCollectionChangedEventArgs e)
    {
      if (e.Action == NotifyCollectionChangedAction.Move)
        return;

      if (e.OldItems != null)
        foreach (LinkedCommand command in e.OldItems)
          switch (command.ExecutionTime)
          {
            case ExecutionTime.Before:
              command.Events.BeforeExecute -= BeforeLinkedCommandExecuted;
              break;
            case ExecutionTime.After:
              command.Events.AfterExecute -= AfterLinkedCommandExecuted;
              break;
          }

      if (e.NewItems != null)
        foreach (LinkedCommand command in e.NewItems)
        {
          command.Events = _dte.Events.CommandEvents[command.Guid, command.Id];

          switch (command.ExecutionTime)
          {
            case ExecutionTime.Before:
              command.Events.BeforeExecute += BeforeLinkedCommandExecuted;
              break;
            case ExecutionTime.After:
              command.Events.AfterExecute += AfterLinkedCommandExecuted;
              break;
          }
        }
    }

    #region insert headers in new files

    private string _currentCommandGuid;
    private int _currentCommandId;
    private CommandEvents _currentCommandEvents;
    private Stack<ProjectItem> _addedItems;

    private void BeforeAnyCommandExecuted (string guid, int id, object customIn, object customOut, ref bool cancelDefault)
    {
      //Save the current command in case it adds a new item to the project.
      _currentCommandGuid = guid;
      _currentCommandId = id;
    }

    private void ItemAdded (ProjectItem item)
    {
      //An item was added. Check if we should insert a header automatically.
      var page = (OptionsPage) GetDialogPage (typeof (OptionsPage));
      if (page != null && page.InsertInNewFiles && item != null)
      {
        //Normally the header should be inserted here, but that might interfere with the command
        //currently being executed, so we wait until it is finished.
        _currentCommandEvents = _dte.Events.CommandEvents[_currentCommandGuid, _currentCommandId];
        _currentCommandEvents.AfterExecute += FinishedAddingItem;
        _addedItems.Push (item);
      }
    }

    private async void FinishedAddingItem (string guid, int id, object customIn, object customOut)
    {
      //Now we can finally insert the header into the new item.

      while (_addedItems.Count > 0)
      {
        var item = _addedItems.Pop();
        var headers = LicenseHeaderFinder.GetHeaderDefinitionForItem (item);
        if (headers != null)
        {
          var result = await LicenseHeaderReplacer.RemoveOrReplaceHeader (
              new LicenseHeaderInput (item.Document.FullName, headers, item.GetAdditionalProperties()),
              false,
              CoreHelpers.NonCommentLicenseHeaderDefinitionInquiry,
              message => CoreHelpers.NoLicenseHeaderDefinitionFound (message, this));
          CoreHelpers.HandleResult(result);
        }
      }

      _currentCommandEvents.AfterExecute -= FinishedAddingItem;
    }

    #endregion

    #endregion

    #region command handlers

    public async Task<ReplacerResult<ReplacerError>> AddLicenseHeaderToItemAsync (ProjectItem item, bool calledByUser)
    {
      if (item == null || ProjectItemInspection.IsLicenseHeader (item))
        return new ReplacerResult<ReplacerError>();

      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
      var headers = LicenseHeaderFinder.GetHeaderDefinitionForItem (item);
      if (headers != null)
        return await LicenseHeaderReplacer.RemoveOrReplaceHeader (
            new LicenseHeaderInput (item.Document.FullName, headers, item.GetAdditionalProperties()),
            calledByUser,
            CoreHelpers.NonCommentLicenseHeaderDefinitionInquiry,
            message => CoreHelpers.NoLicenseHeaderDefinitionFound (message, this));

      var page = (DefaultLicenseHeaderPage) GetDialogPage (typeof (DefaultLicenseHeaderPage));
      if (calledByUser && LicenseHeader.ShowQuestionForAddingLicenseHeaderFile (item.ContainingProject, page))
        return await AddLicenseHeaderToItemAsync (item, true);

      return new ReplacerResult<ReplacerError>();
    }

    public async Task HandleAddLicenseHeaderToAllFilesInProjectReturnAsync (
        object obj,
        AddLicenseHeaderToAllFilesResult addLicenseHeaderToAllFilesResult)
    {
      var project = obj as Project;
      var projectItem = obj as ProjectItem;
      if (project == null && projectItem == null)
        return;
      var currentProject = project;

      if (projectItem != null)
        currentProject = projectItem.ContainingProject;

      if (addLicenseHeaderToAllFilesResult.NoHeaderFound)
      {
        // No license header found...
        var solutionSearcher = new AllSolutionProjectsSearcher();
        var projects = solutionSearcher.GetAllProjects (_dte.Solution);

        if (projects.Any (projectInSolution => LicenseHeaderFinder.GetHeaderDefinitionForProjectWithoutFallback (projectInSolution) != null))
        {
          // If another projet has a license header, offer to add a link to the existing one.
          if (MessageBoxHelper.DoYouWant (Resources.Question_AddExistingDefinitionFileToProject))
          {
            ExistingLicenseHeaderDefinitionFileAdder.AddDefinitionFileToOneProject (currentProject.FileName, currentProject.ProjectItems);
            await FolderProjectMenuHelper.AddLicenseHeaderToAllFilesAsync (this);
          }
        }
        else
        {
          // If no project has a license header, offer to add one for the solution.
          if (MessageBoxHelper.DoYouWant (Resources.Question_AddNewLicenseHeaderDefinitionForSolution))
            AddNewSolutionLicenseHeaderDefinitionFileCallback (this, new EventArgs());
        }
      }
    }

    public async Task HandleLinkedFilesAndShowMessageBox (List<ProjectItem> linkedItems)
    {
      var linkedFileFilter = new LinkedFileFilter (_dte.Solution);
      linkedFileFilter.Filter (linkedItems);

      var linkedFileHandler = new LinkedFileHandler (this);
      await linkedFileHandler.HandleAsync (linkedFileFilter);

      if (linkedFileHandler.Message != string.Empty)
        MessageBox.Show (
            linkedFileHandler.Message,
            Resources.NameOfThisExtension,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void AddNewSolutionLicenseHeaderDefinitionFileCallback (object sender, EventArgs e)
    {
      AddNewSolutionLicenseHeaderDefinitionFileCommand.Instance.Invoke (_dte.Solution);
    }

    #endregion
  }
}