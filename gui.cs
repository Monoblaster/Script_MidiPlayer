//#execOnChange

//TOOD:
// add searching
// finish sorting
// favorites - maybe button switch between history & favorites
// trim history size
// reload button

$Pref::MidiGui::MidiPath = "config/client/midi/";
if(isObject(MidiCtrlGui))
	MidiCtrlGui.delete();

new GuiControl(MidiCtrlGui)
{
	position = "600 600";
	extent = "640 720";
	new GuiWindowCtrl()
	{
		accelerator = "escape";
		command = "canvas.popDialog(MidiCtrlGui);";
		closeCommand = "canvas.popDialog(MidiCtrlGui);";
		canMinimize = 0;
		canMaximize = 0;
		resizeWidth = false;
		resizeHeight = true;
		position = "0 0";
		extent = "640 720";
		new GuiSwatchCtrl()
		{
			position = "0 50";
			vertSizing = "height";
			extent = "483 720";
			color = "255 255 255 0";
			clipToParent = "1";
			new GuiScrollCtrl()
			{
				profile = "GuiScrollProfile";
				horizSizing = "right";
				vertSizing = "height";
				minExtent = "8 2";
				enabled = "1";
				visible = "1";
				clipToParent = "1";
				willFirstRespond = "0";
				hScrollBar = "dynamic";
				vScrollBar = "dynamic";
				constantThumbHeight = "0";
				childMargin = "0 0";
				rowHeight = "40";
				columnWidth = "30";

				position = "3 0";
				extent = "480 670";
				new GuiTextListCtrl(MidiSongsList)
				{
					columns = "0 420 9999";
					position = "0 0";
					extent = "480 670";
				};
			};
		};

		//BUTTONS
		new GuiBitmapButtonCtrl()
		{
			profile = "BlockButtonProfile";
			position = "500 50";
			extent = "110 30";
			minExtent = "8 2";
			command = "MidiGui_PlaySong();";
			text = "Play";
			buttonType = "PushButton";
			bitmap = "base/client/ui/button1";
			alignLeft = "0";
			alignTop = "0";
			overflowImage = "0";
			mKeepCached = "0";
			mColor = "255 200 200 255";
		};

		//BUTTONS
		new GuiBitmapButtonCtrl(MidiGuiSortNameGui)
		{
			profile = "BlockButtonProfile";
			position = "10 28";
			extent = "50 20";
			minExtent = "8 2";
			command = "MidiGui_SortName();";
			text = "Name";
			buttonType = "PushButton";
			bitmap = "base/client/ui/button1";
			alignLeft = "0";
			alignTop = "0";
			overflowImage = "0";
			mKeepCached = "0";
			mColor = "255 200 200 255";
		};
		new GuiBitmapButtonCtrl(MidiGuiSortSizeGui)
		{
			profile = "BlockButtonProfile";
			position = "10 28";
			extent = "50 20";
			minExtent = "8 2";
			command = "MidiGui_SortName();";
			text = "Name";
			buttonType = "PushButton";
			bitmap = "base/client/ui/button1";
			alignLeft = "0";
			alignTop = "0";
			overflowImage = "0";
			mKeepCached = "0";
			mColor = "255 200 200 255";
		};


		//PLAY HISTORY
		new GuiBitmapButtonCtrl()
		{
			profile = "BlockButtonProfile";
			position = "500 90";
			extent = "110 30";
			minExtent = "8 2";
			command = "MidiGui_PlayHistorySong();";
			text = "Play History";
			buttonType = "PushButton";
			bitmap = "base/client/ui/button1";
			//lockAspectRatio = "0";
			alignLeft = "0";
			alignTop = "0";
			overflowImage = "0";
			mKeepCached = "0";
			mColor = "255 200 200 255";
		};

		new GuiSwatchCtrl()
		{
			position = "482 200";
			extent = "155 517";
			color = "255 255 255 0";
			vertSizing = "height";
			new GuiScrollCtrl()
			{
				profile = "GuiScrollProfile";
				horizSizing = "right";
				vertSizing = "height";
				position = "3 3";
				extent = "158 670";
				minExtent = "8 2";
				enabled = "1";
				visible = "1";
				clipToParent = "1";
				willFirstRespond = "0";
				hScrollBar = "dynamic";
				vScrollBar = "dynamic";
				constantThumbHeight = "0";
				childMargin = "0 0";
				rowHeight = "40";
				columnWidth = "30";

				position = "0 0";
				extent = "155 670";
				new GuiTextListCtrl(MidiHistorySongsList)
				{
					columns = "0 999";
					position = "0 0";
					extent = "155 670";
				};
			};
		};
	};
};

function MidiGui_reloadSongs()
{
	setModPaths(getModPaths());
	MidiSongsList.clear();
	for(%file = findFirstFile($Pref::MidiGui::MidiPath @ "*.mid"); %file !$= ""; %file = findNextFile($Pref::MidiGui::MidiPath @ "*.mid"))
	{
		%songName = strreplace(fileBase(%file), "_", " ");
		if(getCharcount(%songName, "-") > 4) //likely to use - as space
			%songName = strreplace(fileBase(%file), "-", " ");
		MidiSongsList.addRow(%songCount++, %songName TAB mFloatLength(getFileLength(%file) / 1024, 0) TAB fileBase(%file));
	}
	MidiSongsList.sort(0, 1);
}

function MidiGui_PlaySong()
{
	%rowID = MidiSongsList.getSelectedRow();
	if(%rowID == -1)
		return;
	
	%songFile = getField(MidiSongsList.getRowText(%rowID), 2);
	MidiPlayer_Play("Instruments", %songFile);

	if(getField(MidiHistorySongsList.getRowText(0), 0) !$= %songFile)
	{
		if(!isObject(MidiGuiFileObject))
			new FileObject(MidiGuiFileObject);

		if(!MidiHistorySongsList.hasLoadedSongs)
		{
			MidiGui_loadSongHistory();
		}
		%file = MidiGuiFileObject;
		%count = MidiHistorySongsList.rowCount();
		MidiHistorySongsList.addRow(%count, %songFile TAB %count);
		MidiHistorySongsList.sortNumerical(1, 0);
		%file.openForAppend($Pref::MidiGui::MidiPath @ "history.tsv");
		%file.writeLine(%songFile);
		%file.close();
	}
}

function MidiGui_PlayHistorySong()
{
	%rowID = MidiHistorySongsList.getSelectedRow();
	if(%rowID == -1)
		return;
	
	%songFile = getField(MidiSongsList.getRowText(%rowID), 2);
	MidiPlayer_Play("Instruments", %songFile);
}

function MidiGui_loadSongHistory()
{
	if(!isFile($Pref::MidiGui::MidiPath @ "history.tsv"))
	{
		MidiHistorySongsList.hasLoadedSongs = true;
		return;
	}

	%file = new FileObject();
	%file.openForRead($Pref::MidiGui::MidiPath @ "history.tsv");
	MidiHistorySongsList.clear();
	while(!%file.isEOF())
	{
		%songFile = %file.readLine();
		%count = MidiHistorySongsList.rowCount();
		MidiHistorySongsList.addRow(%count, %songFile TAB %count);
	}
	%file.close();
	%file.delete();

	MidiHistorySongsList.hasLoadedSongs = true;
	MidiHistorySongsList.sortNumerical(1, 0);
}


MidiGui_loadSongHistory();
MidiGui_reloadSongs();

package MidiPlayerGui
{
	function NMH_Type::send(%this)
	{
		%msg = %this.getValue();
		if(firstWord(%msg) $= "//midi")
		{
			canvas.pushDialog(MidiCtrlGui);
			%this.setValue("");
		}
		parent::send(%this);
	}
};
activatePackage(MidiPlayerGui);