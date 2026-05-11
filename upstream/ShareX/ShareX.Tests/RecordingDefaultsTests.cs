using ShareX.ScreenCaptureLib;
using Xunit;

namespace ShareX.Tests
{
    public class RecordingDefaultsTests
    {
        [Fact]
        public void NewCaptureDefaultsFavorHighQualityRecording()
        {
            TaskSettingsCapture captureSettings = new TaskSettingsCapture();
            FFmpegOptions ffmpeg = captureSettings.FFmpegOptions;

            Assert.Equal(60, captureSettings.ScreenRecordFPS);
            Assert.Equal(24, captureSettings.GIFFPS);
            Assert.Equal(FFmpegPreset.slow, ffmpeg.x264_Preset);
            Assert.Equal(18, ffmpeg.x264_CRF);
            Assert.Equal(16000, ffmpeg.x264_Bitrate);
            Assert.Equal(FFmpegNVENCPreset.p7, ffmpeg.NVENC_Preset);
            Assert.Equal(FFmpegNVENCTune.hq, ffmpeg.NVENC_Tune);
            Assert.Equal(16000, ffmpeg.NVENC_Bitrate);
            Assert.Equal(FFmpegAMFUsage.high_quality, ffmpeg.AMF_Usage);
            Assert.Equal(FFmpegAMFQuality.quality, ffmpeg.AMF_Quality);
            Assert.Equal(16000, ffmpeg.AMF_Bitrate);
            Assert.Equal(FFmpegQSVPreset.veryslow, ffmpeg.QSV_Preset);
            Assert.Equal(16000, ffmpeg.QSV_Bitrate);
            Assert.Equal(192, ffmpeg.AAC_Bitrate);
            Assert.Equal(192, ffmpeg.Opus_Bitrate);
        }

        [Fact]
        public void LegacyRecordingDefaultsUpgradeToProductionProfile()
        {
            TaskSettingsCapture captureSettings = new TaskSettingsCapture
            {
                ScreenRecordFPS = 30,
                GIFFPS = 15,
                FFmpegOptions = new FFmpegOptions
                {
                    x264_Preset = FFmpegPreset.ultrafast,
                    x264_CRF = 28,
                    x264_Bitrate = 3000,
                    VPx_Bitrate = 3000,
                    XviD_QScale = 10,
                    NVENC_Preset = FFmpegNVENCPreset.p4,
                    NVENC_Tune = FFmpegNVENCTune.ll,
                    NVENC_Bitrate = 3000,
                    AMF_Usage = FFmpegAMFUsage.lowlatency,
                    AMF_Quality = FFmpegAMFQuality.speed,
                    AMF_Bitrate = 3000,
                    QSV_Preset = FFmpegQSVPreset.fast,
                    QSV_Bitrate = 3000,
                    AAC_Bitrate = 128,
                    Opus_Bitrate = 128
                }
            };

            bool changed = captureSettings.ApplyProductionRecordingDefaultsIfUsingLegacyDefaults();

            Assert.True(changed);
            Assert.Equal(60, captureSettings.ScreenRecordFPS);
            Assert.Equal(24, captureSettings.GIFFPS);
            Assert.Equal(FFmpegPreset.slow, captureSettings.FFmpegOptions.x264_Preset);
            Assert.Equal(18, captureSettings.FFmpegOptions.x264_CRF);
            Assert.Equal(16000, captureSettings.FFmpegOptions.NVENC_Bitrate);
            Assert.Equal(FFmpegNVENCTune.hq, captureSettings.FFmpegOptions.NVENC_Tune);
        }

        [Fact]
        public void CustomRecordingSettingsAreNotOverwrittenByLegacyUpgrade()
        {
            TaskSettingsCapture captureSettings = new TaskSettingsCapture
            {
                ScreenRecordFPS = 120,
                GIFFPS = 30,
                FFmpegOptions = new FFmpegOptions
                {
                    x264_Preset = FFmpegPreset.medium,
                    x264_CRF = 20,
                    x264_Bitrate = 8000,
                    NVENC_Preset = FFmpegNVENCPreset.p5,
                    NVENC_Tune = FFmpegNVENCTune.lossless,
                    NVENC_Bitrate = 24000,
                    AAC_Bitrate = 256
                }
            };

            bool changed = captureSettings.ApplyProductionRecordingDefaultsIfUsingLegacyDefaults();

            Assert.False(changed);
            Assert.Equal(120, captureSettings.ScreenRecordFPS);
            Assert.Equal(30, captureSettings.GIFFPS);
            Assert.Equal(FFmpegPreset.medium, captureSettings.FFmpegOptions.x264_Preset);
            Assert.Equal(20, captureSettings.FFmpegOptions.x264_CRF);
            Assert.Equal(24000, captureSettings.FFmpegOptions.NVENC_Bitrate);
            Assert.Equal(256, captureSettings.FFmpegOptions.AAC_Bitrate);
        }
    }
}
