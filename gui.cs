//TOOD:
// add searching
// finish sorting
// favorites - maybe button switch between history & favorites
// trim history size
// reload button

$Pref::MidiGui::MidiPath = "config/client/midi/";
exec("./MidiPlayer.gui");
if(!$MidiPlayerGui::hasLoaded)
{
	$RemapDivision[$RemapCount] = "Midi Player";
	$RemapName    [$RemapCount] = "Open Gui";
	$RemapCmd     [$RemapCount] = "MidiGui_openGui";
	$RemapCount++;
	$MidiPlayerGui::hasLoaded = true;
}

function MidiGui_reloadSongs()
{
	setModPaths(getModPaths());
	$MidiSongCount = 0;
	for(%file = findFirstFile($Pref::MidiGui::MidiPath @ "*.mid"); %file !$= ""; %file = findNextFile($Pref::MidiGui::MidiPath @ "*.mid"))
	{
		$MidiSongsList[$MidiSongCount] = %file;
		$MidiSongCount++;
	}

	MidiPlayer_Search();
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
	
	%songFile = getField(MidiHistorySongsList.getRowText(%rowID), 0);
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

function MidiPlayer_Search(%searchString)
{
	MidiSongsList.clear();
	for(%i = 0; %i < $MidiSongCount; %i++)
	{
		%file = $MidiSongsList[%i];
		if(%searchString $= "" || striPos(%file, %searchString) != -1)
		{
			%songName = strreplace(fileBase(%file), "_", " ");
			if(getCharcount(%songName, "-") > 4) //likely to use - as space
				%songName = strreplace(fileBase(%file), "-", " ");
			MidiSongsList.addRow(%i, %songName TAB mFloatLength(getFileLength(%file) / 1024, 0) TAB fileBase(%file));
		}
	}

	MidiPlayer_applySorting();
}

function MidiPlayer_applySorting()
{
	//wip
	MidiSongsList.sort(0, 1);
}

MidiGui_loadSongHistory();
MidiGui_reloadSongs();

function MidiGui_openGui(%value)
{
	if(%value)
	{
		canvas.pushDialog(MidiCtrlGui);
	}
}

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