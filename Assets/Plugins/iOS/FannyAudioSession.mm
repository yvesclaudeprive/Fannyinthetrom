// Fanny in 2 Trom : le son doit marcher même quand l'iPhone est en mode silencieux.
// Par défaut, Unity utilise la catégorie audio "Ambient" (coupée par le bouton silencieux) :
// on passe en "Playback", sans couper la musique des autres apps.
#import <AVFoundation/AVFoundation.h>

extern "C" void FannySetPlaybackAudio()
{
    NSError *error = nil;
    AVAudioSession *session = [AVAudioSession sharedInstance];
    [session setCategory:AVAudioSessionCategoryPlayback
                    mode:AVAudioSessionModeDefault
                 options:AVAudioSessionCategoryOptionMixWithOthers
                   error:&error];
    [session setActive:YES error:&error];
}
