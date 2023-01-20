using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UI;
/*
	File browser for selecting files or folders at runtime.
 */

public enum FileBrowserType {
	File,
	Directory
}
 
public class FileBrowser : IDisposable {
 
	// Called when the user clicks cancel or select
	public delegate void FinishedCallback(string path);
	// Defaults to working directory
	public string CurrentDirectory {
		get {
			return m_currentDirectory;
		}
		set {
			SetNewDirectory(value);
			SwitchDirectoryNow();
		}
	}
	protected string m_currentDirectory;
	// Optional pattern for filtering selectable files/folders. See:
	// http://msdn.microsoft.com/en-us/library/wz42302f(v=VS.90).aspx
	// and
	// http://msdn.microsoft.com/en-us/library/6ff71z1w(v=VS.90).aspx
	public string SelectionPattern {
		get {
			return m_filePattern;
		}
		set {
			m_filePattern = value;
			//ReadDirectoryContents();
		}
	}
	protected string m_filePattern;
 
	// Optional image for directories
	public Texture2D DirectoryImage {
		get {
			return m_directoryImage;
		}
		set {
			m_directoryImage = value;
			BuildContent();
		}
	}
	protected Texture2D m_directoryImage;
 
	// Optional image for files
	public Texture2D FileImage {
		get {
			return m_fileImage;
		}
		set {
			m_fileImage = value;
			BuildContent();
		}
	}
	protected Texture2D m_fileImage;
 
	// Browser type. Defaults to File, but can be set to Folder
	public FileBrowserType BrowserType {
		get {
			return m_browserType;
		}
		set {
			m_browserType = value;
			//ReadDirectoryContents();
		}
	}
	protected FileBrowserType m_browserType;
	protected string m_newDirectory;
	protected string[] m_currentDirectoryParts;
 
	protected string[] m_files;
	protected GUIContent[] m_filesWithImages;
	protected int m_selectedFile;
 
	protected string[] m_nonMatchingFiles;
	protected GUIContent[] m_nonMatchingFilesWithImages;
	protected int m_selectedNonMatchingDirectory;
 
	protected string[] m_directories;
	protected GUIContent[] m_directoriesWithImages;
	protected int m_selectedDirectory;
 
	protected string[] m_nonMatchingDirectories;
	protected GUIContent[] m_nonMatchingDirectoriesWithImages;
 
	protected bool m_currentDirectoryMatches;
 
	protected GUIStyle CentredText {
		get {
			if (m_centredText == null) {
				m_centredText = new GUIStyle(GUI.skin.label);
				m_centredText.alignment = TextAnchor.MiddleLeft;
				m_centredText.fixedHeight = GUI.skin.button.fixedHeight;
			}
			return m_centredText;
		}
	}
	protected GUIStyle m_centredText;
 
	protected string m_name;
	protected Rect m_screenRect;
 
	protected Vector2 m_scrollPosition;
 
	protected FinishedCallback m_callback;

	protected FileListItem itemprefab;
	protected FileListItem itemPrefabFolder;
	protected VerticalLayoutGroup layout;
	protected FileListItem.ItemClickDelegate OnItemClicked;
	protected AddressItem.AddressItemClickedDelegate OnItemAddressClicked;
	protected List<FileListItem> displayItemList;
	protected List<AddressItem> displayItemAddressList;

	protected AddressItem addressItemPrefab;
	protected HorizontalLayoutGroup layoutAddress;


	// Browsers need at least a rect, name and callback
	public FileBrowser(Rect screenRect, string name, string currentdirectory, FinishedCallback callback) {
		m_name = name;
		m_screenRect = screenRect;
		m_browserType = FileBrowserType.File;
		m_callback = callback;
		SetNewDirectory(currentdirectory);
		SwitchDirectoryNow();
	}

	public FileBrowser(Rect screenRect, string name, string currentdirectory, FinishedCallback callback, FileListItem itemprefab, FileListItem itemPrefabFolder, AddressItem addressItemPrefab, VerticalLayoutGroup layout, HorizontalLayoutGroup layoutAddress, string pattern, FileListItem.ItemClickDelegate itemClickedCalback, AddressItem.AddressItemClickedDelegate addressClickedDelegate)
	{
		this.layout = layout;
		this.itemprefab = itemprefab;
		this.itemPrefabFolder = itemPrefabFolder;
		this.layoutAddress = layoutAddress;
		this.addressItemPrefab = addressItemPrefab;

		this.SelectionPattern = pattern;
		this.OnItemClicked = itemClickedCalback;
		this.OnItemAddressClicked = addressClickedDelegate;
		displayItemList = new List<FileListItem>();
		displayItemAddressList = new List<AddressItem>();
		//if(pattern != null)
		//{
		BrowserType = FileBrowserType.File;
        //}

		if (layout!=null)
        {
			FileListItem[] items = layout.GetComponentsInChildren<FileListItem>();
			foreach(FileListItem item in items)
            {
				GameObject.Destroy(item.gameObject);
			}
		}

		m_name = name;
		m_screenRect = screenRect;
		m_callback = callback;
		SetNewDirectory(currentdirectory);
		SwitchDirectoryNow();
	}

	protected void SetNewDirectory(string directory) {
		m_newDirectory = directory;
	}
 
	protected void SwitchDirectoryNow() {
		if (m_newDirectory == null || m_currentDirectory == m_newDirectory) {
			return;
		}
		m_currentDirectory = m_newDirectory;
		m_scrollPosition = Vector2.zero;
		m_selectedDirectory = m_selectedNonMatchingDirectory = m_selectedFile = -1;
		ReadDirectoryContents();
	}

	protected void ReadDirectoryContents()
	{
		if (m_currentDirectory == "/")
		{
			m_currentDirectoryParts = new string[] { "" };
			m_currentDirectoryMatches = false;
		}
		else if (m_currentDirectory == string.Empty)
		{
			m_currentDirectoryParts = new string[0];
			m_currentDirectoryMatches = false;
		}
		else
		{
			m_currentDirectoryParts = m_currentDirectory.Split(Path.DirectorySeparatorChar);
			if (SelectionPattern != null)
			{
				//string[] generation = Directory.GetDirectories(Path.GetDirectoryName(m_currentDirectory),SelectionPattern);
				string directoryName = Path.GetDirectoryName(m_currentDirectory);
				string[] generation = new string[0];
				if (directoryName != null)
				{   //This is new: generation should be an empty array for the root directory.
					//directoryName will be null if it's a root directory
					generation = Directory.GetDirectories(directoryName, SelectionPattern);
				}
				m_currentDirectoryMatches = Array.IndexOf(generation, m_currentDirectory) >= 0;

			}
			else
			{
				m_currentDirectoryMatches = false;
			}
		}

		if (m_currentDirectory == string.Empty)
		{
			m_directories = Directory.GetLogicalDrives();
			m_nonMatchingDirectories = new string[0];
		}
		else
		{
			if (BrowserType == FileBrowserType.File || SelectionPattern == null)
			{
				m_directories = Directory.GetDirectories(m_currentDirectory);
				m_nonMatchingDirectories = new string[0];
			}
			else
			{
				m_directories = Directory.GetDirectories(m_currentDirectory);
				var nonMatchingDirectories = new List<string>();
				foreach (string directoryPath in Directory.GetDirectories(m_currentDirectory))
				{
					if (Array.IndexOf(m_directories, directoryPath) < 0)
					{
						nonMatchingDirectories.Add(directoryPath);
					}
				}
				m_nonMatchingDirectories = nonMatchingDirectories.ToArray();
				for (int i = 0; i < m_nonMatchingDirectories.Length; ++i)
				{
					int lastSeparator = m_nonMatchingDirectories[i].LastIndexOf(Path.DirectorySeparatorChar);
					m_nonMatchingDirectories[i] = m_nonMatchingDirectories[i].Substring(lastSeparator + 1);
				}
				Array.Sort(m_nonMatchingDirectories);
			}
			for (int i = 0; i < m_directories.Length; ++i)
			{
				m_directories[i] = m_directories[i].Substring(m_directories[i].LastIndexOf(Path.DirectorySeparatorChar) + 1);
			}
		}

		if (m_currentDirectory == string.Empty)
		{
			m_files = new string[0];
			m_nonMatchingFiles = new string[0];
		}
		else
		{
			if (BrowserType == FileBrowserType.Directory || SelectionPattern == null)
			{
				m_files = Directory.GetFiles(m_currentDirectory);
				m_nonMatchingFiles = new string[0];
			}
			else
			{
				m_files = Directory.GetFiles(m_currentDirectory, SelectionPattern);
				var nonMatchingFiles = new List<string>();
				foreach (string filePath in Directory.GetFiles(m_currentDirectory))
				{
					if (Array.IndexOf(m_files, filePath) < 0)
					{
						nonMatchingFiles.Add(filePath);
					}
				}
				m_nonMatchingFiles = nonMatchingFiles.ToArray();
				for (int i = 0; i < m_nonMatchingFiles.Length; ++i)
				{
					m_nonMatchingFiles[i] = Path.GetFileName(m_nonMatchingFiles[i]);
				}
				Array.Sort(m_nonMatchingFiles);
			}
			for (int i = 0; i < m_files.Length; ++i)
			{
				m_files[i] = Path.GetFileName(m_files[i]);
			}
			Array.Sort(m_files);
		}

		//BuildContent();
		BuildContentFromPrefav(m_currentDirectory);
		BuildAddressBar();
		m_newDirectory = null;
	}
 
	protected void BuildContent() {
		m_directoriesWithImages = new GUIContent[m_directories.Length];
		for (int i = 0; i < m_directoriesWithImages.Length; ++i) {
			m_directoriesWithImages[i] = new GUIContent(m_directories[i], DirectoryImage);
		}
		m_nonMatchingDirectoriesWithImages = new  GUIContent[m_nonMatchingDirectories.Length];
		for (int i = 0; i < m_nonMatchingDirectoriesWithImages.Length; ++i) {
			m_nonMatchingDirectoriesWithImages[i] = new GUIContent(m_nonMatchingDirectories[i], DirectoryImage);
		}
		m_filesWithImages = new GUIContent[m_files.Length];
		for (int i = 0; i < m_filesWithImages.Length; ++i) {
			m_filesWithImages[i] = new GUIContent(m_files[i], FileImage);
		}
		m_nonMatchingFilesWithImages = new GUIContent[m_nonMatchingFiles.Length];
		for (int i = 0; i < m_nonMatchingFilesWithImages.Length; ++i) {
			m_nonMatchingFilesWithImages[i] = new GUIContent(m_nonMatchingFiles[i], FileImage);
		}
	}

	protected void BuildContentFromPrefav(string currentDirectory)
	{
		//FileListItem itemprefab;
		//VerticalLayoutGroup layout;

		/*m_directoriesWithImages = new GUIContent[m_directories.Length];
		for (int i = 0; i < m_directoriesWithImages.Length; ++i)
		{
			//m_directoriesWithImages[i] = new GUIContent(m_directories[i], DirectoryImage);

		}
		m_nonMatchingDirectoriesWithImages = new GUIContent[m_nonMatchingDirectories.Length];
		for (int i = 0; i < m_nonMatchingDirectoriesWithImages.Length; ++i)
		{
			m_nonMatchingDirectoriesWithImages[i] = new GUIContent(m_nonMatchingDirectories[i], DirectoryImage);
		}
		m_filesWithImages = new GUIContent[m_files.Length];
		for (int i = 0; i < m_filesWithImages.Length; ++i)
		{
			m_filesWithImages[i] = new GUIContent(m_files[i], FileImage);
		}*/

		if (layout != null)
		{
			for (int i = 0; i < m_files.Length; ++i)
			{
				FileListItem item = GameObject.Instantiate<FileListItem>(itemprefab);
				item.transform.parent = layout.transform;
				item.Text = currentDirectory + Path.DirectorySeparatorChar + m_files[i];
				item.Tmpro.text = m_files[i];
				item.Index = i;
				item.Type = FileListItem.ItemType.File;

				if (OnItemClicked!=null)
                {
					item.OnItemClick += OnItemClicked;
				}

				displayItemList.Add(item);


			}

			for (int i = 0; i < m_directories.Length; ++i)
			{
				FileListItem item = GameObject.Instantiate<FileListItem>(itemPrefabFolder);
				item.transform.parent = layout.transform;
				item.Text = currentDirectory + Path.DirectorySeparatorChar + m_directories[i] +Path.DirectorySeparatorChar;
				item.Tmpro.text = m_directories[i];
				item.Index = i;
				item.Type = FileListItem.ItemType.Folder;

				if (OnItemClicked != null)
				{
					item.OnItemClick += OnItemClicked;
				}

				displayItemList.Add(item);
			}

		}


	}

	protected void BuildAddressBar()
    {
		if (m_currentDirectoryParts.Length == 0)
		{ // special case: drive selection
			//GUILayout.Label("PC", CentredText);

		}
		else
		{
			if (Directory.GetLogicalDrives().Length >= 1)
			{ // go to drive selection if possible
				//if (GUILayout.Button("PC"))
				//{
					//SetNewDirectory(string.Empty);
				//}

				AddressItem item = GameObject.Instantiate(addressItemPrefab);
				item.transform.parent = layoutAddress.transform;
				item.TMProUI.text = "MyPC";

				if (OnItemAddressClicked != null)
				{
					item.OnClicked += OnItemAddressClicked;
				}

				item.Text = "MyPC";
				displayItemAddressList.Add(item);
			}



			for (int parentIndex = 0; parentIndex < m_currentDirectoryParts.Length; ++parentIndex)
			{
				AddressItem item = GameObject.Instantiate(addressItemPrefab);
				item.transform.parent = layoutAddress.transform;
				item.TMProUI.text = m_currentDirectoryParts[parentIndex];


				if (OnItemAddressClicked != null)
				{
					item.OnClicked += OnItemAddressClicked;
				}

				displayItemAddressList.Add(item);

				/*if (parentIndex == m_currentDirectoryParts.Length - 1)
				{
					GUILayout.Label(m_currentDirectoryParts[parentIndex], CentredText);
				}
				else if (GUILayout.Button(m_currentDirectoryParts[parentIndex]))
				{
					string parentDirectoryName = m_currentDirectory;
					for (int i = m_currentDirectoryParts.Length - 1; i > parentIndex; --i)
					{
						parentDirectoryName = Path.GetDirectoryName(parentDirectoryName);
					}
					SetNewDirectory(parentDirectoryName);
				}*/
			}


			for (int parentIndex = 0; parentIndex < m_currentDirectoryParts.Length; ++parentIndex)
			{
				string parentDirectoryName = m_currentDirectory;
				for (int i = displayItemAddressList.Count - 1; i >=parentIndex; --i)
				{
					parentDirectoryName = Path.GetDirectoryName(parentDirectoryName);
				}

				displayItemAddressList[parentIndex].Text = parentDirectoryName;

			}


		}
	}

	public void OnDestroy()
    {
		foreach (FileListItem item in displayItemList)
		{
			item.OnItemClick -= OnItemClicked;
			GameObject.Destroy(item.gameObject);
		}

		foreach (AddressItem item in displayItemAddressList)
		{
			item.OnClicked -= OnItemAddressClicked;
			GameObject.Destroy(item.gameObject);
		}
	}

	public void Refresh()
    {
		foreach (FileListItem item in displayItemList)
		{
			item.OnItemClick -= OnItemClicked;
			GameObject.Destroy(item.gameObject);
		}

		foreach (AddressItem item in displayItemAddressList)
		{
			item.OnClicked -= OnItemAddressClicked;
			GameObject.Destroy(item.gameObject);
		}

		displayItemList.Clear();
		displayItemAddressList.Clear();

	}
 
	public void OnGUI() {
		
		GUI.enabled = true;
		GUILayout.BeginArea(m_screenRect,m_name,GUI.skin.window);
			GUILayout.BeginHorizontal();
		if (m_currentDirectoryParts.Length == 0)
		{ // special case: drive selection
			GUILayout.Label("PC", CentredText);
		}
		else
		{
			if (Directory.GetLogicalDrives().Length > 1)
			{ // go to drive selection if possible
				if (GUILayout.Button("PC"))
				{
					SetNewDirectory(string.Empty);
				}
			}
			for (int parentIndex = 0; parentIndex < m_currentDirectoryParts.Length; ++parentIndex)
			{
				if (parentIndex == m_currentDirectoryParts.Length - 1)
				{
					GUILayout.Label(m_currentDirectoryParts[parentIndex], CentredText);
				}
				else if (GUILayout.Button(m_currentDirectoryParts[parentIndex]))
				{
					string parentDirectoryName = m_currentDirectory;
					for (int i = m_currentDirectoryParts.Length - 1; i > parentIndex; --i)
					{
						parentDirectoryName = Path.GetDirectoryName(parentDirectoryName);
					}
					SetNewDirectory(parentDirectoryName);
				}
			}
		}
				GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			m_scrollPosition = GUILayout.BeginScrollView(
				m_scrollPosition,
				false,
				true,
				GUI.skin.horizontalScrollbar,
				GUI.skin.verticalScrollbar,
				GUI.skin.box
			);
			//	---------------------------
			//
		   m_selectedDirectory = GUILayoutx.SelectionList(
					m_selectedDirectory,
					m_directoriesWithImages,
					DirectoryDoubleClickCallback
				);
				if (m_selectedDirectory > -1) {
					m_selectedFile = m_selectedNonMatchingDirectory = -1;
				}
				m_selectedNonMatchingDirectory = GUILayoutx.SelectionList(
					m_selectedNonMatchingDirectory,
					m_nonMatchingDirectoriesWithImages,
					NonMatchingDirectoryDoubleClickCallback
				);
				if (m_selectedNonMatchingDirectory > -1) {
					m_selectedDirectory = m_selectedFile = -1;
				}
				GUI.enabled = BrowserType == FileBrowserType.File;
				m_selectedFile = GUILayoutx.SelectionList(
					m_selectedFile,
					m_filesWithImages,
					FileDoubleClickCallback
				);
				GUI.enabled = true;
				if (m_selectedFile > -1) {
					m_selectedDirectory = m_selectedNonMatchingDirectory = -1;
				}
				GUI.enabled = false;
				GUILayoutx.SelectionList(
					-1,
					m_nonMatchingFilesWithImages
				);
				GUI.enabled = true;
			GUILayout.EndScrollView();
			GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Cancel", GUILayout.Width(50))) {
					m_callback(null);
				}
				if (BrowserType == FileBrowserType.File) {
					GUI.enabled = m_selectedFile > -1;
				} else {
					if (SelectionPattern == null) {
						GUI.enabled = m_selectedDirectory > -1;
					} else {
						GUI.enabled =	m_selectedDirectory > -1 ||
										(
											m_currentDirectoryMatches &&
											m_selectedNonMatchingDirectory == -1 &&
											m_selectedFile == -1
										);
					}
				}
				if (GUILayout.Button("Select", GUILayout.Width(50))) {
					if (BrowserType == FileBrowserType.File) {
						m_callback(Path.Combine(m_currentDirectory, m_files[m_selectedFile]));
					} else {
						if (m_selectedDirectory > -1) {
							m_callback(Path.Combine(m_currentDirectory, m_directories[m_selectedDirectory]));
						} else {
							m_callback(m_currentDirectory);
						}
					}
				}
				GUI.enabled = true;
			GUILayout.EndHorizontal();
		GUILayout.EndArea();
 
		if (Event.current.type == EventType.Repaint) {
			SwitchDirectoryNow();
		}
	}

	protected void FileDoubleClickCallback(int i)
	{
		if (BrowserType == FileBrowserType.File)
		{
			m_callback(Path.Combine(m_currentDirectory, m_files[i]));
		}
	}

	protected void DirectoryDoubleClickCallback(int i)
	{
		SetNewDirectory(Path.Combine(m_currentDirectory, m_directories[i]));
	}

	protected void NonMatchingDirectoryDoubleClickCallback(int i)
	{
		SetNewDirectory(Path.Combine(m_currentDirectory, m_nonMatchingDirectories[i]));
	}

    public void Dispose()
    {
		//Pass true in dispose method to clean managed resources too and say GC to skip finalize in next line.
		Dispose(true);
		//If dispose is called already then say GC to skip finalize on this instance.
		GC.SuppressFinalize(this);
	}

    ~FileBrowser()
    {
		foreach (FileListItem item in displayItemList)
		{
			item.OnItemClick -= OnItemClicked;
			GameObject.Destroy(item.gameObject);
		}

		foreach(AddressItem item in displayItemAddressList)
        {
			//item.OnItemClick -= OnItemClicked;
			GameObject.Destroy(item.gameObject);
		}

		Dispose(false);
	}

	private bool IsDisposed = false;

	//Implement dispose to free resources
	protected virtual void Dispose(bool disposedStatus)
	{
		if (!IsDisposed)
		{
			IsDisposed = true;
			// Released unmanaged Resources
			if (disposedStatus)
			{
				// Released managed Resources
			}
		}
	}

}