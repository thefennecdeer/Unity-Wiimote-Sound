using System;
using UnityEngine;

namespace WiimoteApi.Util {

public static class AudioConverter {

	private static NAudio.Dsp.WdlResampler resampler = null;
	private static float[] temp_samples = new float[0];
	private static void InitResampler(){
		resampler = new NAudio.Dsp.WdlResampler();
		resampler.SetMode(true, 4, true);
		resampler.SetFeedMode(true);
	}

	public static ReadOnlySpan<float> Resample(ReadOnlySpan<float> samples, double rate_in, double rate_out){
		if (resampler == null)
			InitResampler();
		resampler.Reset();
		resampler.SetRates(rate_in, rate_out);
		resampler.ResamplePrepare(samples.Length, 1, out float[] buf, out int buf_offset);
		samples.CopyTo(new Span<float>(buf));
		int num_out_samples = (int)Math.Ceiling((double)samples.Length * (rate_out / rate_in));
		if (temp_samples.Length < num_out_samples){
			Array.Resize(ref temp_samples, num_out_samples);
		}
		num_out_samples = resampler.ResampleOut(temp_samples, 0, samples.Length, num_out_samples, 1);
		if (num_out_samples != temp_samples.Length)
			Array.Resize(ref temp_samples, num_out_samples);

		return new ReadOnlySpan<float>(temp_samples);
	}

	private static readonly int[] index_table = new int[16]{  -1,  -1,  -1,  -1,   2,   4,   6,   8,
										-1,  -1,  -1,  -1,   2,   4,   6,   8 };
	private static readonly int[] diff_table  = new int[16]{   1,   3,   5,   7,   9,  11,  13,  15,
										-1,  -3,  -5,  -7,  -9, -11, -13,  15 };
	private static readonly int[] step_scale = new int[16]{ 230, 230, 230, 230, 307, 409, 512, 614,
											230, 230, 230, 230, 307, 409, 512, 614 };

	public static byte[] ConvertSamplesToADPCM(ReadOnlySpan<float> samples){
		// copied from https://wiiyourself.gl.tter.org/

		int adpcm_prev_value = 0;
		int adpcm_step = 127;

		// ADPCM code, adapted from
		//  http://www.wiindows.org/index.php/Talk:Wiimote#Input.2FOutput_Reports
		// Encode to ADPCM, on initialization set adpcm_prev_value to 0 and adpcm_step
		//  to 127 (these variables must be preserved across reports)

		int length = samples.Length;

		var result = new byte[(length + 1) >> 1];

		for (int i = 0; i < length; i++){
			int value = (int)(Mathf.Clamp(samples[i], -1.0f, 1.0f) * 32767.0f);

			int diff = value - adpcm_prev_value;
			byte encoded_val = 0;
			if (diff < 0) {
				encoded_val |= 8;
				diff = -diff;
			}

			diff = (diff << 2) / adpcm_step;
			if (diff > 7)
				diff = 7;
			encoded_val |= (byte)diff;

			adpcm_prev_value += ((adpcm_step * diff_table[encoded_val]) / 8);
			if (adpcm_prev_value > 0x7fff)
				adpcm_prev_value = 0x7fff;
			else if (adpcm_prev_value < -0x8000)
				adpcm_prev_value = -0x8000;

			adpcm_step = (adpcm_step * step_scale[encoded_val]) >> 8;
			if (adpcm_step < 127)
				adpcm_step = 127;
			else if (adpcm_step > 0x6000)
				adpcm_step = 0x6000;

			if ((i & 1) == 0)
				encoded_val <<= 4;

			result[i >> 1] |= encoded_val;
		}

		return result;
	}
}

}
