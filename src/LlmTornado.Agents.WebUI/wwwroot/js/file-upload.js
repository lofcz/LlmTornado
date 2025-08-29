// File upload helper functions for LlmTornado Chat

window.fileUploadHelper = {
    /**
     * Opens a file dialog and converts the selected file to Base64 with MIME type
     * @param {string} acceptTypes - Accepted file types (e.g., "image/*", ".pdf", etc.)
     * @returns {Promise<string|null>} Base64 string in format "data:{mimeType};base64,{base64String}" or null if cancelled
     */
    selectAndConvertFile: async function(acceptTypes = "*/*") {
        return new Promise((resolve) => {
            // Create a temporary file input element
            const input = document.createElement('input');
            input.type = 'file';
            input.accept = acceptTypes;
            input.style.display = 'none';
            
            input.onchange = async function(event) {
                const file = event.target.files[0];
                if (!file) {
                    resolve(null);
                    return;
                }
                
                try {
                    const result = await fileUploadHelper.fileToBase64WithMimeType(file);
                    resolve(result);
                } catch (error) {
                    console.error('Error converting file to Base64:', error);
                    resolve(null);
                } finally {
                    // Clean up
                    document.body.removeChild(input);
                }
            };
            
            input.oncancel = function() {
                document.body.removeChild(input);
                resolve(null);
            };
            
            // Add to DOM and trigger click
            document.body.appendChild(input);
            input.click();
        });
    },

    /**
     * Converts a file input element to Base64
     * @param {HTMLInputElement} inputElement - The file input element
     * @returns {Promise<string|null>} Base64 string with MIME type or null if no file
     */
    convertFileToBase64: async function(inputElement) {
        if (!inputElement.files || inputElement.files.length === 0) {
            return null;
        }
        
        const file = inputElement.files[0];
        return await this.fileToBase64WithMimeType(file);
    },

    /**
     * Converts a File object to Base64 with MIME type prefix
     * @param {File} file - The file to convert
     * @returns {Promise<string>} Base64 string in format "data:{mimeType};base64,{base64String}"
     */
    fileToBase64WithMimeType: function(file) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            
            reader.onload = function(event) {
                // FileReader result includes the data URL with MIME type
                resolve(event.target.result);
            };
            
            reader.onerror = function(error) {
                reject(error);
            };
            
            // Read as data URL (includes MIME type)
            reader.readAsDataURL(file);
        });
    },

    /**
     * Validates file size and type
     * @param {File} file - The file to validate
     * @param {number} maxSizeInMB - Maximum file size in MB
     * @param {string[]} allowedTypes - Array of allowed MIME types
     * @returns {object} Validation result with isValid and message properties
     */
    validateFile: function(file, maxSizeInMB = 10, allowedTypes = []) {
        if (!file) {
            return { isValid: false, message: "No file selected" };
        }

        // Check file size
        const maxSizeInBytes = maxSizeInMB * 1024 * 1024;
        if (file.size > maxSizeInBytes) {
            return { 
                isValid: false, 
                message: `File size (${(file.size / 1024 / 1024).toFixed(1)} MB) exceeds maximum allowed size (${maxSizeInMB} MB)` 
            };
        }

        // Check file type if specified
        if (allowedTypes.length > 0) {
            const isAllowed = allowedTypes.some(type => {
                if (type.endsWith('/*')) {
                    return file.type.startsWith(type.slice(0, -1));
                }
                return file.type === type;
            });

            if (!isAllowed) {
                return { 
                    isValid: false, 
                    message: `File type (${file.type}) is not allowed. Allowed types: ${allowedTypes.join(', ')}` 
                };
            }
        }

        return { isValid: true, message: "File is valid" };
    },

    /**
     * Formats file size for display
     * @param {number} sizeInBytes - File size in bytes
     * @returns {string} Formatted file size
     */
    formatFileSize: function(sizeInBytes) {
        if (sizeInBytes < 1024) return sizeInBytes + ' B';
        if (sizeInBytes < 1024 * 1024) return (sizeInBytes / 1024).toFixed(1) + ' KB';
        if (sizeInBytes < 1024 * 1024 * 1024) return (sizeInBytes / (1024 * 1024)).toFixed(1) + ' MB';
        return (sizeInBytes / (1024 * 1024 * 1024)).toFixed(1) + ' GB';
    },

    /**
     * Gets file type category for UI display
     * @param {string} mimeType - MIME type of the file
     * @returns {string} File category (image, document, audio, video, other)
     */
    getFileCategory: function(mimeType) {
        if (mimeType.startsWith('image/')) return 'image';
        if (mimeType.startsWith('audio/')) return 'audio';
        if (mimeType.startsWith('video/')) return 'video';
        if (mimeType === 'application/pdf' || 
            mimeType.includes('document') || 
            mimeType.includes('text')) return 'document';
        return 'other';
    }
};