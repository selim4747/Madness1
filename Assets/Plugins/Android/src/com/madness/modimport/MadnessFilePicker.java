package com.madness.modimport;

import android.app.Activity;
import android.content.Intent;
import android.database.Cursor;
import android.net.Uri;
import android.os.Bundle;
import android.provider.OpenableColumns;
import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;

public final class MadnessFilePicker extends Activity {
    private static final int REQUEST_PICK = 18427;
    private static volatile String pendingPath = "";
    private static volatile String pendingError = "";

    public static void open(Activity caller) {
        pendingPath = "";
        pendingError = "";
        Intent intent = new Intent(caller, MadnessFilePicker.class);
        caller.startActivity(intent);
    }

    public static String pollPath() {
        String value = pendingPath;
        pendingPath = "";
        return value;
    }

    public static String pollError() {
        String value = pendingError;
        pendingError = "";
        return value;
    }

    @Override
    protected void onCreate(Bundle state) {
        super.onCreate(state);
        try {
            Intent picker = new Intent(Intent.ACTION_OPEN_DOCUMENT);
            picker.addCategory(Intent.CATEGORY_OPENABLE);
            picker.setType("*/*");
            startActivityForResult(picker, REQUEST_PICK);
        } catch (Exception error) {
            pendingError = "Android dosya seçici açılamadı.";
            finish();
        }
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode != REQUEST_PICK) return;
        if (resultCode != RESULT_OK || data == null || data.getData() == null) {
            pendingError = "Dosya seçilmedi.";
            finish();
            return;
        }
        try {
            pendingPath = copyToCache(data.getData());
        } catch (Exception error) {
            pendingError = "Seçilen mod dosyası okunamadı.";
        }
        finish();
    }

    private String copyToCache(Uri uri) throws Exception {
        File target = new File(getCacheDir(), "madness_mod_" + System.currentTimeMillis() + ".zip");
        InputStream input = getContentResolver().openInputStream(uri);
        if (input == null) throw new Exception("Dosya akışı açılamadı");
        FileOutputStream output = new FileOutputStream(target);
        byte[] buffer = new byte[8192];
        int read;
        long total = 0;
        while ((read = input.read(buffer)) != -1) {
            total += read;
            if (total > 300L * 1024L * 1024L) throw new Exception("Dosya çok büyük");
            output.write(buffer, 0, read);
        }
        input.close();
        output.close();
        return target.getAbsolutePath();
    }
}
