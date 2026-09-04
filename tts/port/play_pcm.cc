/* play_pcm.cc (port) — WAV-file output replacement for the ALSA player.
 * Same signature as the vendor play_pcm.cc so the main.cc call site is
 * untouched; writes PCM16 WAV instead of opening a sound device. The
 * RIFF/FMT/DATA structs come from the vendor play_pcm.h. */
#include "play_pcm.h"

#include <cstdio>
#include <cstdlib>

int playerPcm(int channels, unsigned int &sample_rate, char *mydata, unsigned int &data_len)
{
    const char *path = getenv("TTS_OUT");
    if (!path || !*path) path = "./tts_out.wav";

    FILE *fp = fopen(path, "wb");
    if (!fp) {
        fprintf(stderr, "playerPcm: cannot open %s\n", path);
        return -1;
    }

    const unsigned int bits = 16;
    const unsigned int byte_rate = sample_rate * channels * bits / 8;
    const unsigned int block_align = channels * bits / 8;

    RIFF_HEADER riff = {{'R', 'I', 'F', 'F'}, 36 + data_len, {'W', 'A', 'V', 'E'}};
    FMT_BLOCK fmt = {{'f', 'm', 't', ' '}, 16,
                     {1, (WORD)channels, sample_rate, byte_rate,
                      (WORD)block_align, (WORD)bits}};
    DATA_BLOCK data = {{'d', 'a', 't', 'a'}, data_len};

    fwrite(&riff, sizeof(riff), 1, fp);
    fwrite(&fmt, sizeof(fmt), 1, fp);
    fwrite(&data, sizeof(data), 1, fp);
    fwrite(mydata, 1, data_len, fp);
    fclose(fp);

    fprintf(stderr, "playerPcm: wrote %s (%u bytes, %u Hz, %d ch)\n",
            path, data_len, sample_rate, channels);
    return 0;
}
