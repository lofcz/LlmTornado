using System;
using System.Collections.Generic;
using System.Linq;

namespace LlmTornado.Code.MimeTypeMap;

internal static class MagicByteDefinitions
{
    static MagicByteDefinitions()
    {
        Sig("gif", ["0x47", "0x49", "0x46", "0x38", "0x37", "0x61"], new Info { Mime = "image/gif", Extension = "gif" });
        Sig("gif", ["0x47", "0x49", "0x46", "0x38", "0x39", "0x61"], new Info { Mime = "image/gif", Extension = "gif" });
        Sig("jpg", ["0xFF", "0xD8", "0xFF"], new Info { Mime = "image/jpeg", Extension = "jpeg" });
        Sig("webp", ["0x52", "0x49", "0x46", "0x46", "?", "?", "?", "?", "0x57", "0x45", "0x42", "0x50"], new Info { Mime = "image/webp", Extension = "webp" });
        Sig("heif", ["0x66", "0x74", "0x79", "0x70", "0x6D", "0x69", "0x66", "0x31"], new Info { Mime = "image/heif", Extension = "heif" }, 4);
        Sig("heif", ["0x66", "0x74", "0x79", "0x70", "0x68", "0x65", "0x69", "0x63"], new Info { Mime = "image/heif", Extension = "heic" }, 4);
        Sig("rpm", ["0xed", "0xab", "0xee", "0xdb"]);
        Sig("bin", ["0x53", "0x50", "0x30", "0x31"], new Info { Mime = "application/octet-stream", Extension = "bin" });
        Sig("pic", ["0x00"]);
        Sig("pif", ["0x00"]);
        Sig("sea", ["0x00"]);
        Sig("ytr", ["0x00"]);
        Sig("mp4", ["0x66", "0x74", "0x79", "0x70"], new Info { Mime = "video/mp4", Extension = "mp4" }, 0x4);
        Sig("ttf", ["0x00", "0x01", "0x00", "0x00", "0x00"], new Info { Mime = "font/ttf", Extension = "ttf" });
        Sig("otf", ["0x4F", "0x54", "0x54", "0x4F"], new Info { Mime = "font/otf", Extension = "otf" });
        Sig("eot", ["0x50", "0x4C"], new Info { Mime = "application/vnd.ms-fontobject", Extension = "eot" });
        Sig("woff", ["0x77", "0x4F", "0x46", "0x46"], new Info { Mime = "font/woff", Extension = "woff" });
        Sig("woff2", ["0x77", "0x4F", "0x46", "0x32"], new Info { Mime = "font/woff2", Extension = "woff2" });
        Sig("pdb", ["0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00"]);
        Sig("dba", ["0xBE", "0xBA", "0xFE", "0xCA"]);
        Sig("dba2", ["0x00", "0x01", "0x42", "0x44"]);
        Sig("tda", ["0x00", "0x01", "0x44", "0x54"]);
        Sig("tda2", ["0x00", "0x01", "0x00", "0x00"]);
        Sig("ico", ["0x00", "0x00", "0x01", "0x00"], new Info { Mime = "image/x-icon", Extension = "ico" });
        Sig("3gp", ["0x66", "0x74", "0x79", "0x70", "0x33", "0x67"]);
        Sig("z", ["0x1F", "0x9D"]);
        Sig("tar.z", ["0x1F", "0xA0"]);
        Sig("bac", ["0x42", "0x41", "0x43", "0x4B", "0x4D", "0x49", "0x4B", "0x45", "0x44", "0x49", "0x53", "0x4B"]);
        Sig("bz2", ["0x42", "0x5A", "0x68"], new Info { Mime = "application/x-bzip2", Extension = "bz2" });
        Sig("tif", ["0x49", "0x49", "0x2A", "0x00"], new Info { Mime = "image/tiff", Extension = "tif" });
        Sig("tiff", ["0x4D", "0x4D", "0x00", "0x2A"], new Info { Mime = "image/tiff", Extension = "tiff" });
        Sig("cr2", ["0x49", "0x49", "0x2A", "0x00", "0x10", "0x00", "0x00", "0x00", "0x43", "0x52"]);
        Sig("cin", ["0x80", "0x2A", "0x5F", "0xD7"]);
        Sig("cin1", ["0x52", "0x4E", "0x43", "0x01"]);
        Sig("cin2", ["0x52", "0x4E", "0x43", "0x02"]);
        Sig("dpx", ["0x53", "0x44", "0x50", "0x58"]);
        Sig("dpx2", ["0x58", "0x50", "0x44", "0x53"]);
        Sig("exr", ["0x76", "0x2F", "0x31", "0x01"]);
        Sig("bpg", ["0x42", "0x50", "0x47", "0xFB"]);
        Sig("ilbm", ["0x46", "0x4F", "0x52", "0x4D", "?", "?", "?", "?", "0x49", "0x4C", "0x42", "0x4D"]);
        Sig("8svx", ["0x46", "0x4F", "0x52", "0x4D", "?", "?", "?", "?", "0x38", "0x53", "0x56", "0x58"]);
        Sig("acbm", ["0x46", "0x4F", "0x52", "0x4D", "?", "?", "?", "?", "0x41", "0x43", "0x42", "0x4D"]);
        Sig("anbm", ["0x46", "0x4F", "0x52", "0x4D", "?", "?", "?", "?", "0x41", "0x4E", "0x42", "0x4D"]);
        Sig("anim", ["0x46", "0x4F", "0x52", "0x4D", "?", "?", "?", "?", "0x41", "0x4E", "0x49", "0x4D"]);
        Sig("faxx", ["0x46", "0x4F", "0x52", "0x4D", "?", "?", "?", "?", "0x46", "0x41", "0x58", "0x58"]);
        Sig("ftxt", ["0x46", "0x4F", "0x52", "0x4D", "?", "?", "?", "?", "0x46", "0x54", "0x58", "0x54"]);
        Sig("smus", ["0x46", "0x4F", "0x52", "0x4D", "?", "?", "?", "?", "0x53", "0x4D", "0x55", "0x53"]);
        Sig("cmus", ["0x46", "0x4F", "0x52", "0x4D", "?", "?", "?", "?", "0x43", "0x4D", "0x55", "0x53"]);
        Sig("yuvn", ["0x46", "0x4F", "0x52", "0x4D", "?", "?", "?", "?", "0x59", "0x55", "0x56", "0x4E"]);
        Sig("iff", ["0x46", "0x4F", "0x52", "0x4D", "?", "?", "?", "?", "0x46", "0x41", "0x4E", "0x54"]);
        Sig("aiff", ["0x46", "0x4F", "0x52", "0x4D", "?", "?", "?", "?", "0x41", "0x49", "0x46", "0x46"], new Info { Mime = "audio/x-aiff", Extension = "aiff" });
        Sig("idx", ["0x49", "0x4E", "0x44", "0x58"]);
        Sig("lz", ["0x4C", "0x5A", "0x49", "0x50"]);
        Sig("exe", ["0x4D", "0x5A"]);
        Sig("zip", ["0x50", "0x4B", "0x03", "0x04"], new Info { Mime = "application/zip", Extension = "zip" });
        Sig("zip", ["0x50", "0x4B", "0x05", "0x06"], new Info { Mime = "application/zip", Extension = "zip" });
        Sig("zip", ["0x50", "0x4B", "0x07", "0x08"], new Info { Mime = "application/zip", Extension = "zip" });
        Sig("jar", ["0x50", "0x4B", "0x03", "0x04"], new Info { Mime = "application/java-archive", Extension = "jar" });
        Sig("jar", ["0x50", "0x4B", "0x05", "0x06"], new Info { Mime = "application/java-archive", Extension = "jar" });
        Sig("jar", ["0x50", "0x4B", "0x07", "0x08"], new Info { Mime = "application/java-archive", Extension = "jar" });
        Sig("odt", ["0x50", "0x4B", "0x03", "0x04"], new Info { Mime = "application/vnd.oasis.opendocument.text", Extension = "odt" });
        Sig("odt", ["0x50", "0x4B", "0x05", "0x06"], new Info { Mime = "application/vnd.oasis.opendocument.text", Extension = "odt" });
        Sig("odt", ["0x50", "0x4B", "0x07", "0x08"], new Info { Mime = "application/vnd.oasis.opendocument.text", Extension = "odt" });
        Sig("ods", ["0x50", "0x4B", "0x03", "0x04"], new Info { Mime = "application/vnd.oasis.opendocument.spreadsheet", Extension = "ods" });
        Sig("ods", ["0x50", "0x4B", "0x05", "0x06"], new Info { Mime = "application/vnd.oasis.opendocument.spreadsheet", Extension = "ods" });
        Sig("ods", ["0x50", "0x4B", "0x07", "0x08"], new Info { Mime = "application/vnd.oasis.opendocument.spreadsheet", Extension = "ods" });
        Sig("odp", ["0x50", "0x4B", "0x03", "0x04"], new Info { Mime = "application/vnd.oasis.opendocument.presentation", Extension = "odp" });
        Sig("odp", ["0x50", "0x4B", "0x05", "0x06"], new Info { Mime = "application/vnd.oasis.opendocument.presentation", Extension = "odp" });
        Sig("odp", ["0x50", "0x4B", "0x07", "0x08"], new Info { Mime = "application/vnd.oasis.opendocument.presentation", Extension = "odp" });
        Sig("docx", ["0x50", "0x4B", "0x03", "0x04"], new Info { Mime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document", Extension = "docx" });
        Sig("docx", ["0x50", "0x4B", "0x05", "0x06"], new Info { Mime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document", Extension = "docx" });
        Sig("docx", ["0x50", "0x4B", "0x07", "0x08"], new Info { Mime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document", Extension = "docx" });
        Sig("xlsx", ["0x50", "0x4B", "0x03", "0x04"], new Info { Mime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", Extension = "xlsx" });
        Sig("xlsx", ["0x50", "0x4B", "0x05", "0x06"], new Info { Mime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", Extension = "xlsx" });
        Sig("xlsx", ["0x50", "0x4B", "0x07", "0x08"], new Info { Mime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", Extension = "xlsx" });
        Sig("pptx", ["0x50", "0x4B", "0x03", "0x04"], new Info { Mime = "application/vnd.openxmlformats-officedocument.presentationml.presentation", Extension = "pptx" });
        Sig("pptx", ["0x50", "0x4B", "0x05", "0x06"], new Info { Mime = "application/vnd.openxmlformats-officedocument.presentationml.presentation", Extension = "pptx" });
        Sig("pptx", ["0x50", "0x4B", "0x07", "0x08"], new Info { Mime = "application/vnd.openxmlformats-officedocument.presentationml.presentation", Extension = "pptx" });
        Sig("vsdx", ["0x50", "0x4B", "0x03", "0x04"], new Info { Mime = "application/vnd.ms-visio.drawing", Extension = "vsdx" });
        Sig("vsdx", ["0x50", "0x4B", "0x05", "0x06"], new Info { Mime = "application/vnd.ms-visio.drawing", Extension = "vsdx" });
        Sig("vsdx", ["0x50", "0x4B", "0x07", "0x08"], new Info { Mime = "application/vnd.ms-visio.drawing", Extension = "vsdx" });
        Sig("apk", ["0x50", "0x4B", "0x03", "0x04"], new Info { Mime = "application/vnd.android.package-archive", Extension = "apk" });
        Sig("apk", ["0x50", "0x4B", "0x05", "0x06"], new Info { Mime = "application/vnd.android.package-archive", Extension = "apk" });
        Sig("apk", ["0x50", "0x4B", "0x07", "0x08"], new Info { Mime = "application/vnd.android.package-archive", Extension = "apk" });
        Sig("aar", ["0x50", "0x4B", "0x03", "0x04"], new Info { Mime = "application/vnd.android.package-archive", Extension = "aar" });
        Sig("aar", ["0x50", "0x4B", "0x05", "0x06"], new Info { Mime = "application/vnd.android.package-archive", Extension = "aar" });
        Sig("aar", ["0x50", "0x4B", "0x07", "0x08"], new Info { Mime = "application/vnd.android.package-archive", Extension = "aar" });
        Sig("rar", ["0x52", "0x61", "0x72", "0x21", "0x1A", "0x07", "0x00"], new Info { Mime = "application/vnd.rar", Extension = "rar" });
        Sig("rar", ["0x52", "0x61", "0x72", "0x21", "0x1A", "0x07", "0x01", "0x00"], new Info { Mime = "application/vnd.rar", Extension = "rar" });
        Sig("png", ["0x89", "0x50", "0x4E", "0x47", "0x0D", "0x0A", "0x1A", "0x0A"], new Info { Mime = "image/png", Extension = "png" });
        Sig("apng", ["0x89", "0x50", "0x4E", "0x47", "0x0D", "0x0A", "0x1A", "0x0A"], new Info { Mime = "image/apng", Extension = "apng" });
        Sig("class", ["0xCA", "0xFE", "0xBA", "0xBE"]);
        Sig("ps", ["0x25", "0x21", "0x50", "0x53"], new Info { Mime = "application/postscript", Extension = ".ps" });
        Sig("pdf", ["0x25", "0x50", "0x44", "0x46"], new Info { Mime = "application/pdf", Extension = "pdf" });
        Sig("asf", ["0x30", "0x26", "0xB2", "0x75", "0x8E", "0x66", "0xCF", "0x11", "0xA6", "0xD9", "0x00", "0xAA", "0x00", "0x62", "0xCE", "0x6C"]);
        Sig("wma", ["0x30", "0x26", "0xB2", "0x75", "0x8E", "0x66", "0xCF", "0x11", "0xA6", "0xD9", "0x00", "0xAA", "0x00", "0x62", "0xCE", "0x6C"]);
        Sig("wmv", ["0x30", "0x26", "0xB2", "0x75", "0x8E", "0x66", "0xCF", "0x11", "0xA6", "0xD9", "0x00", "0xAA", "0x00", "0x62", "0xCE", "0x6C"]);
        Sig("deploymentimage", ["0x24", "0x53", "0x44", "0x49", "0x30", "0x30", "0x30", "0x31"]);
        Sig("ogv", ["0x4F", "0x67", "0x67", "0x53", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "0x80", "0x74", "0x68", "0x65", "0x6F", "0x72", "0x61"], new Info { Mime = "video/ogg", Extension = "ogv" });
        Sig("ogm", ["0x4F", "0x67", "0x67", "0x53", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "0x01", "0x76", "0x69", "0x64", "0x65", "0x6F", "0x00"], new Info { Mime = "video/ogg", Extension = "ogm" });
        Sig("oga", ["0x4F", "0x67", "0x67", "0x53", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "0x7F", "0x46", "0x4C", "0x41", "0x43"], new Info { Mime = "audio/ogg", Extension = "oga" });
        Sig("spx", ["0x4F", "0x67", "0x67", "0x53", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "0x53", "0x70", "0x65", "0x65", "0x78", "0x20", "0x20"], new Info { Mime = "audio/ogg", Extension = "spx" });
        Sig("ogg", ["0x4F", "0x67", "0x67", "0x53", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "?", "0x01", "0x76", "0x6F", "0x72", "0x62", "0x69", "0x73"], new Info { Mime = "audio/ogg", Extension = "ogg" });
        Sig("ogx", ["0x4F", "0x67", "0x67", "0x53"], new Info { Mime = "application/ogg", Extension = "ogx" });
        Sig("psd", ["0x38", "0x42", "0x50", "0x53"], new Info { Mime = "application/x-photoshop", Extension = "psd" });
        Sig("clip", ["0x43", "0x53", "0x46", "0x43", "0x48", "0x55", "0x4e", "0x4b"]);
        Sig("wav", ["0x52", "0x49", "0x46", "0x46", "?", "?", "?", "?", "0x57", "0x41", "0x56", "0x45"], new Info { Mime = "audio/x-wav", Extension = "wav" });
        Sig("avi", ["0x52", "0x49", "0x46", "0x46", "?", "?", "?", "?", "0x41", "0x56", "0x49", "0x20"], new Info { Mime = "video/x-msvideo", Extension = "avi" });
        Sig("mp3", ["0xFF", "0xFB"], new Info { Mime = "audio/mpeg", Extension = "mp3" });
        Sig("mp3", ["0xFF", "0xF3"], new Info { Mime = "audio/mpeg", Extension = "mp3" });
        Sig("mp3", ["0xFF", "0xF2"], new Info { Mime = "audio/mpeg", Extension = "mp3" });
        Sig("mp3", ["0x49", "0x44", "0x33"], new Info { Mime = "audio/mpeg", Extension = "mp3" });
        Sig("bmp", ["0x42", "0x4D"], new Info { Mime = "image/bmp", Extension = "bmp" });
        Sig("iso", ["0x43", "0x44", "0x30", "0x30", "0x31"]);
        Sig("flac", ["0x66", "0x4C", "0x61", "0x43"]);
        Sig("mid", ["0x4D", "0x54", "0x68", "0x64"], new Info { Mime = "audio/midi", Extension = "mid" });
        Sig("midi", ["0x4D", "0x54", "0x68", "0x64"], new Info { Mime = "audio/midi", Extension = "midi" });
        Sig("doc", ["0xD0", "0xCF", "0x11", "0xE0", "0xA1", "0xB1", "0x1A", "0xE1"], new Info { Mime = "application/msword", Extension = "doc" });
        Sig("xls", ["0xD0", "0xCF", "0x11", "0xE0", "0xA1", "0xB1", "0x1A", "0xE1"], new Info { Mime = "application/vnd.ms-excel", Extension = "xls" });
        Sig("ppt", ["0xD0", "0xCF", "0x11", "0xE0", "0xA1", "0xB1", "0x1A", "0xE1"], new Info { Mime = "application/vnd.ms-powerpoint", Extension = "ppt" });
        Sig("msg", ["0xD0", "0xCF", "0x11", "0xE0", "0xA1", "0xB1", "0x1A", "0xE1"]);
        Sig("dex", ["0x64", "0x65", "0x78", "0x0A", "0x30", "0x33", "0x35", "0x00"]);
        Sig("vmdk", ["0x4B", "0x44", "0x4D"]);
        Sig("crx", ["0x43", "0x72", "0x32", "0x34"]);
        Sig("fh8", ["0x41", "0x47", "0x44", "0x33"]);
        Sig("cwk", ["0x05", "0x07", "0x00", "0x00", "0x42", "0x4F", "0x42", "0x4F", "0x05", "0x07", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x01"]);
        Sig("cwk", ["0x06", "0x07", "0xE1", "0x00", "0x42", "0x4F", "0x42", "0x4F", "0x06", "0x07", "0xE1", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x00", "0x01"]);
        Sig("toast", ["0x45", "0x52", "0x02", "0x00", "0x00", "0x00"]);
        Sig("toast", ["0x8B", "0x45", "0x52", "0x02", "0x00", "0x00", "0x00"]);
        Sig("dmg", ["0x78", "0x01", "0x73", "0x0D", "0x62", "0x62", "0x60"]);
        Sig("xar", ["0x78", "0x61", "0x72", "0x21"]);
        Sig("dat", ["0x50", "0x4D", "0x4F", "0x43", "0x43", "0x4D", "0x4F", "0x43"]);
        Sig("nes", ["0x4E", "0x45", "0x53", "0x1A"]);
        Sig("tar", ["0x75", "0x73", "0x74", "0x61", "0x72", "0x00", "0x30", "0x30"], new Info { Mime = "application/x-tar", Extension = "tar" }, 0x101);
        Sig("tar", ["0x75", "0x73", "0x74", "0x61", "0x72", "0x20", "0x20", "0x00"], new Info { Mime = "application/x-tar", Extension = "tar" }, 0x101);
        Sig("tox", ["0x74", "0x6F", "0x78", "0x33"]);
        Sig("mlv", ["0x4D", "0x4C", "0x56", "0x49"]);
        Sig("windowsupdate", ["0x44", "0x43", "0x4D", "0x01", "0x50", "0x41", "0x33", "0x30"]);
        Sig("7z", ["0x37", "0x7A", "0xBC", "0xAF", "0x27", "0x1C"], new Info { Mime = "application/x-7z-compressed", Extension = "7z" });
        Sig("gz", ["0x1F", "0x8B"], new Info { Mime = "application/gzip", Extension = "gz" });
        Sig("tar.gz", ["0x1F", "0x8B"], new Info { Mime = "application/gzip", Extension = "tar.gz" });
        Sig("xz", ["0xFD", "0x37", "0x7A", "0x58", "0x5A", "0x00", "0x00"], new Info { Mime = "application/gzip", Extension = "xz" });
        Sig("tar.xz", ["0xFD", "0x37", "0x7A", "0x58", "0x5A", "0x00", "0x00"], new Info { Mime = "application/gzip", Extension = "tar.xz" });
        Sig("lz2", ["0x04", "0x22", "0x4D", "0x18"]);
        Sig("cab", ["0x4D", "0x53", "0x43", "0x46"]);
        Sig("mkv", ["0x1A", "0x45", "0xDF", "0xA3"], new Info { Mime = "video/x-matroska", Extension = "mkv" });
        Sig("mka", ["0x1A", "0x45", "0xDF", "0xA3"], new Info { Mime = "audio/x-matroska", Extension = "mka" });
        Sig("mks", ["0x1A", "0x45", "0xDF", "0xA3"], new Info { Mime = "video/x-matroska", Extension = "mks" });
        Sig("mk3d", ["0x1A", "0x45", "0xDF", "0xA3"]);
        Sig("webm", ["0x1A", "0x45", "0xDF", "0xA3"], new Info { Mime = "audio/webm", Extension = "webm" });
        Sig("dcm", ["0x44", "0x49", "0x43", "0x4D"], null, 0x80);
        Sig("xml", ["0x3C", "0x3f", "0x78", "0x6d", "0x6C", "0x20"], new Info { Mime = "application/xml", Extension = "xml" });
        Sig("wasm", ["0x00", "0x61", "0x73", "0x6d"], new Info { Mime = "application/wasm", Extension = "wasm" });
        Sig("lep", ["0xCF", "0x84", "0x01"]);
        Sig("swf", ["0x43", "0x57", "0x53"], new Info { Mime = "application/x-shockwave-flash", Extension = "swf" });
        Sig("swf", ["0x46", "0x57", "0x53"], new Info { Mime = "application/x-shockwave-flash", Extension = "swf" });
        Sig("deb", ["0x21", "0x3C", "0x61", "0x72", "0x63", "0x68", "0x3E"]);
        Sig("rtf", ["0x7B", "0x5C", "0x72", "0x74", "0x66", "0x31"], new Info { Mime = "application/rtf", Extension = "rtf" });
        Sig("m2p", ["0x00", "0x00", "0x01", "0xBA"]);
        Sig("vob", ["0x00", "0x00", "0x01", "0xBA"]);
        Sig("mpg", ["0x00", "0x00", "0x01", "0xBA"], new Info { Mime = "video/mpeg", Extension = "mpg" });
        Sig("mpeg", ["0x00", "0x00", "0x01", "0xBA"], new Info { Mime = "video/mpeg", Extension = "mpeg" });
        Sig("mpeg", ["0x47"], new Info { Mime = "video/mpeg", Extension = "mpeg" }); // MPEG Transport Stream (TS)
        Sig("mpeg", ["0x00", "0x00", "0x01", "0xB3"], new Info { Mime = "video/mpeg", Extension = "mpeg" });
        Sig("mov", ["0x66", "0x72", "0x65", "0x65"], new Info { Mime = "video/quicktime", Extension = "mov" }, 0x4);
        Sig("mov", ["0x6D", "0x64", "0x61", "0x74"], new Info { Mime = "video/quicktime", Extension = "mov" }, 0x4);
        Sig("mov", ["0x6D", "0x6F", "0x6F", "0x76"], new Info { Mime = "video/quicktime", Extension = "mov" }, 0x4);
        Sig("mov", ["0x77", "0x69", "0x64", "0x65"], new Info { Mime = "video/quicktime", Extension = "mov" }, 0x4);
        Sig("mov", ["0x66", "0x74", "0x79", "0x70", "0x71", "0x74"], new Info { Mime = "video/quicktime", Extension = "mov" }, 0x4);
        Sig("hl2demo", ["0x48", "0x4C", "0x32", "0x44", "0x45", "0x4D", "0x4F"]);
        Sig("txt", ["0xEF", "0xBB", "0xBF"], new Info { Mime = "text/plain; charset=UTF-8", Extension = "txt" });
        Sig("txt", ["0xFF", "0xFE"], new Info { Mime = "text/plain; charset=UTF-16LE", Extension = "txt" });
        Sig("txt", ["0xFE", "0xFF"], new Info { Mime = "text/plain; charset=UTF-16BE", Extension = "txt" });
        Sig("txt", ["0xFF", "0xFE", "0x00", "0x00"], new Info { Mime = "text/plain; charset=UTF-32LE", Extension = "txt" });
        Sig("txt", ["0x00", "0x00", "0xFE", "0xFF"], new Info { Mime = "text/plain; charset=UTF-32BE", Extension = "txt" });
        Sig("SubRip", ["0x31", "0x0D", "0x0A", "0x30", "0x30", "0x3A"], new Info { Mime = "application/x-subrip", Extension = "srt" });
        Sig("WebVTT", ["0xEF", "0xBB", "0xBF", "0x57", "0x45", "0x42", "0x56", "0x54", "0x54", "0x0A"], new Info { Mime = "text/vtt", Extension = "vtt" });
        Sig("WebVTT", ["0xEF", "0xBB", "0xBF", "0x57", "0x45", "0x42", "0x56", "0x54", "0x54", "0x0D"], new Info { Mime = "text/vtt", Extension = "vtt" });
        Sig("WebVTT", ["0xEF", "0xBB", "0xBF", "0x57", "0x45", "0x42", "0x56", "0x54", "0x54", "0x20"], new Info { Mime = "text/vtt", Extension = "vtt" });
        Sig("WebVTT", ["0xEF", "0xBB", "0xBF", "0x57", "0x45", "0x42", "0x56", "0x54", "0x54", "0x09"], new Info { Mime = "text/vtt", Extension = "vtt" });
        Sig("WebVTT", ["0x57", "0x45", "0x42", "0x56", "0x54", "0x54", "0x0A"], new Info { Mime = "text/vtt", Extension = "vtt" });
        Sig("WebVTT", ["0x57", "0x45", "0x42", "0x56", "0x54", "0x54", "0x0D"], new Info { Mime = "text/vtt", Extension = "vtt" });
        Sig("WebVTT", ["0x57", "0x45", "0x42", "0x56", "0x54", "0x54", "0x20"], new Info { Mime = "text/vtt", Extension = "vtt" });
        Sig("WebVTT", ["0x57", "0x45", "0x42", "0x56", "0x54", "0x54", "0x09"], new Info { Mime = "text/vtt", Extension = "vtt" });
        Sig("Json", ["0x7B"], new Info { Mime = "application/json", Extension = ".json" });
        Sig("Json", ["0x5B"], new Info { Mime = "application/json", Extension = ".json" });
        Sig("ELF", ["0x7F", "0x45", "0x4C", "0x46"], new Info { Mime = "application/x-executable", Extension = "elf" });
        Sig("Mach-O", ["0xFE", "0xED", "0xFA", "0xCE"], new Info { Mime = "application/x-mach-binary", Extension = "o" });
        Sig("Mach-O", ["0xFE", "0xED", "0xFA", "0xCF"], new Info { Mime = "application/x-mach-binary", Extension = "o" });
        Sig("Mach-O", ["0xCE", "0xFA", "0xED", "0xFE"], new Info { Mime = "application/x-mach-binary", Extension = "o" });
        Sig("Mach-O", ["0xCF", "0xFA", "0xED", "0xFE"], new Info { Mime = "application/x-mach-binary", Extension = "o" });
        Sig("Mach-O", ["0xFE", "0xED", "0xFA", "0xCE"], new Info { Mime = "application/x-mach-binary", Extension = "o" }, 0x1000);
        Sig("Mach-O", ["0xFE", "0xED", "0xFA", "0xCF"], new Info { Mime = "application/x-mach-binary", Extension = "o" }, 0x1000);
        Sig("EML", ["0x52", "0x65", "0x63", "0x65", "0x69", "0x76", "0x65", "0x64", "0x3A"], new Info { Mime = "message/rfc822", Extension = ".eml" });
        Sig("SVG", ["0x3c", "0x73", "0x76", "0x67"], new Info { Mime = "image/svg+xml", Extension = "svg" });
        Sig("avif", ["0x66", "0x74", "0x79", "0x70", "0x61", "0x76", "0x69", "0x66"], new Info { Mime = "image/avif", Extension = "avif" }, 4);
    }
    
    private static readonly SignatureTree tree = new SignatureTree();

    private static int ParseSignatureByte(string hex)
    {
        if (hex == "?")
        {
            return -1;
        }

        return Convert.ToInt32(hex, 16);
    }

    /// <summary>
    /// Adds a file type signature to the global signature tree. Use ? as a wildcard byte.
    /// </summary>
    public static void Sig(string typename, string[] signature, Info? additionalInfo = null, int offset = 0)
    {
        SignatureNode currentRoot;

        if (offset == 0)
        {
            currentRoot = tree.NoOffsetRoot;
        }
        else
        {
            if (!tree.OffsetRoots.TryGetValue(offset, out currentRoot))
            {
                currentRoot = new SignatureNode();
                tree.OffsetRoots[offset] = currentRoot;
            }
        }

        SignatureNode currentNode = currentRoot;
        foreach (string hexByte in signature)
        {
            int byteValue = ParseSignatureByte(hexByte);
            if (!currentNode.Children.TryGetValue(byteValue, out SignatureNode? nextNode))
            {
                nextNode = new SignatureNode();
                currentNode.Children[byteValue] = nextNode;
            }

            currentNode = nextNode;
        }

        additionalInfo ??= new Info();
        additionalInfo.TypeName = typename;
        currentNode.Matches.Add(additionalInfo);
    }

    public static SignatureTree GetSignatureTree() => tree;
}

static class MagicByteDetector
{
    private static readonly SignatureTree _signatureTree = MagicByteDefinitions.GetSignatureTree();

    static MagicByteDetector()
    {
        int maxOffset = 0;
        int maxSignatureLength = 0;

        CalculateMaxDepth(_signatureTree.NoOffsetRoot, 0, ref maxSignatureLength);

        foreach (KeyValuePair<int, SignatureNode> entry in _signatureTree.OffsetRoots)
        {
            maxOffset = Math.Max(maxOffset, entry.Key);
            int currentSignatureLength = 0;
            CalculateMaxDepth(entry.Value, 0, ref currentSignatureLength);
            maxSignatureLength = Math.Max(maxSignatureLength, currentSignatureLength);
        }

        MaxBytesToRead = maxOffset + maxSignatureLength;

        // more than 8 KB should never be needed
        MaxBytesToRead = Math.Max(MaxBytesToRead, 8192);
    }

    private static void CalculateMaxDepth(SignatureNode node, int currentDepth, ref int maxDepth)
    {
        maxDepth = Math.Max(maxDepth, currentDepth);
        foreach (SignatureNode? child in node.Children.Values)
        {
            CalculateMaxDepth(child, currentDepth + 1, ref maxDepth);
        }
    }

    public static List<Info> Detect(byte[] headerBytes)
    {
        List<Info> potentialMatches = [];

        MatchSignature(headerBytes, _signatureTree.NoOffsetRoot, 0, potentialMatches);

        foreach (KeyValuePair<int, SignatureNode> entry in _signatureTree.OffsetRoots)
        {
            MatchSignature(headerBytes, entry.Value, entry.Key, potentialMatches);
        }

#if NET6_0_OR_GREATER
                return potentialMatches.DistinctBy(i => i.TypeName).ToList();
#else
        return potentialMatches
            .GroupBy(i => i.TypeName)
            .Select(g => g.First())
            .ToList();
#endif
    }

    private static void MatchSignature(byte[] headerBytes, SignatureNode root, int offset, List<Info> matches)
    {
        if (offset < 0 || offset >= headerBytes.Length) return;

        SignatureNode currentNode = root;
        for (int i = offset; i < headerBytes.Length; i++)
        {
            byte currentByte = headerBytes[i];

            if (currentNode.Children.TryGetValue(currentByte, out SignatureNode? nextNodeExact))
            {
                currentNode = nextNodeExact;
            }
            else if (currentNode.Children.TryGetValue(-1, out SignatureNode? nextNodeWildcard))
            {
                currentNode = nextNodeWildcard;
            }
            else
            {
                return;
            }

            if (currentNode.Matches.Count > 0)
            {
                matches.AddRange(currentNode.Matches);
            }
        }
    }

    public static int MaxBytesToRead { get; }
}

internal class Info
{
    public string TypeName { get; set; } = string.Empty;
    public string? Mime { get; set; }
    public string? Extension { get; set; }
}

internal class SignatureNode
{
    public Dictionary<int, SignatureNode> Children { get; } = new Dictionary<int, SignatureNode>();
    public List<Info> Matches { get; } = [];
}

internal class SignatureTree
{
    public SignatureNode NoOffsetRoot { get; } = new SignatureNode();
    public Dictionary<int, SignatureNode> OffsetRoots { get; } = [];
}