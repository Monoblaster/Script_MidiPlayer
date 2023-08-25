luaexec("./midiplayer.lua");

function clientCmdCustomMidiPlayerNext()
{
	talk("next");
	luacall("MidiPlayer_Next");
}

$MidiPlayer::LastStop = 0;
package MidiPlayer
{
	function clientCmdStopPlayingInstrument()
	{	
		parent::clientCmdStopPlayingInstrument();
		if(($MidiPlayer::LastStop + 1000) < getSimTime())
		{
			luacall("MidiPlayer_Next");
		}
		$MidiPlayer::LastStop = getSimTime();
	}
};
activatePackage("MidiPlayer");

function MidiPlayer_Play(%method,%file,%minutes)
{
	%file = findFirstFile("config/client/midi/" @ %file @ "*.mid");	
	echo(%file);
	if(%file !$= "")
	{
		if(%method $= "Instruments")
		{
			luacall("MidiPlayer_PlayInstruments",%file,%minutes * 60000);
			return;
		}

		if(%method $= "Custom")
		{
			luacall("MidiPlayer_PlayCustom",%file,%minutes * 60000);
			return;
		}
	}
	echo("not found!");
}