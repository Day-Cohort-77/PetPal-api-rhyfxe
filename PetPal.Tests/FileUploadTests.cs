using Microsoft.EntityFrameworkCore;
using PetPal.API.Data;
using PetPal.API.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace PetPal.Tests;

public class FileUploadTests : IDisposable
{
    private readonly PetPalDbContext _context;

    public FileUploadTests()
    {
        var options = new DbContextOptionsBuilder<PetPalDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PetPalDbContext(options);
        _context.Database.EnsureCreated();
    }

    #region File Upload Validation Tests
    [Fact]
    public void ValidateImageFile_ValidPngFile_ShouldReturnTrue()
    {
        // Arrange
        var validPngBytes = CreateTestImageBytes();
        var fileName = "test.png";

        // Act
        var result = IsValidImageFile(validPngBytes, fileName);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateImageFile_ValidJpegFile_ShouldReturnTrue()
    {
        // Arrange
        var validJpegBytes = CreateTestJpegBytes();
        var fileName = "test.jpg";

        // Act
        var result = IsValidImageFile(validJpegBytes, fileName);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateImageFile_InvalidFileExtension_ShouldReturnFalse()
    {
        // Arrange
        var imageBytes = CreateTestImageBytes();
        var fileName = "test.txt";

        // Act
        var result = IsValidImageFile(imageBytes, fileName);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateImageFile_InvalidMagicBytes_ShouldReturnFalse()
    {
        // Arrange
        var invalidBytes = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var fileName = "test.png";

        // Act
        var result = IsValidImageFile(invalidBytes, fileName);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("image.png", true)]
    [InlineData("image.jpg", true)]
    [InlineData("image.jpeg", true)]
    [InlineData("image.gif", true)]
    [InlineData("image.bmp", false)]
    [InlineData("document.pdf", false)]
    [InlineData("file.txt", false)]
    [InlineData("", false)]
    public void ValidateFileExtension_WithDifferentExtensions_ShouldReturnExpectedResult(string fileName, bool expected)
    {
        // Act
        var result = HasValidImageExtension(fileName);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ValidateFileSize_WithinLimit_ShouldReturnTrue()
    {
        // Arrange
        var fileBytes = new byte[1024 * 1024]; // 1MB
        var maxSizeInBytes = 5 * 1024 * 1024; // 5MB

        // Act
        var result = IsValidFileSize(fileBytes.Length, maxSizeInBytes);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateFileSize_ExceedsLimit_ShouldReturnFalse()
    {
        // Arrange
        var fileSize = 6 * 1024 * 1024; // 6MB
        var maxSizeInBytes = 5 * 1024 * 1024; // 5MB

        // Act
        var result = IsValidFileSize(fileSize, maxSizeInBytes);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1024, true)]
    [InlineData(1024 * 1024, true)] // 1MB
    [InlineData(5 * 1024 * 1024, true)] // 5MB - exact limit
    [InlineData(5 * 1024 * 1024 + 1, false)] // Just over 5MB
    [InlineData(10 * 1024 * 1024, false)] // 10MB
    public void ValidateFileSize_WithVariousSizes_ShouldReturnExpectedResult(int fileSize, bool expected)
    {
        // Arrange
        var maxSizeInBytes = 5 * 1024 * 1024; // 5MB

        // Act
        var result = IsValidFileSize(fileSize, maxSizeInBytes);

        // Assert
        result.Should().Be(expected);
    }
    #endregion

    #region File Path Generation Tests
    [Fact]
    public void GenerateUniqueFileName_WithValidInput_ShouldCreateUniqueFileName()
    {
        // Arrange
        var originalFileName = "test-image.png";

        // Act
        var result1 = GenerateUniqueFileName(originalFileName);
        var result2 = GenerateUniqueFileName(originalFileName);

        // Assert
        result1.Should().NotBeNullOrEmpty();
        result2.Should().NotBeNullOrEmpty();
        result1.Should().NotBe(result2); // Should be unique
        result1.Should().EndWith(".png");
        result2.Should().EndWith(".png");
        result1.Should().NotContain("test-image"); // Should not contain original name for security
    }

    [Theory]
    [InlineData("image.PNG", ".png")]
    [InlineData("IMAGE.JPG", ".jpg")]
    [InlineData("test.JPEG", ".jpeg")]
    [InlineData("file.gif", ".gif")]
    public void GenerateUniqueFileName_WithDifferentCases_ShouldNormalizeExtension(string fileName, string expectedExtension)
    {
        // Act
        var result = GenerateUniqueFileName(fileName);

        // Assert
        result.Should().EndWith(expectedExtension);
    }

    [Fact]
    public void GenerateUploadPath_WithValidFileName_ShouldCreateValidPath()
    {
        // Arrange
        var fileName = "abc123.png";
        var uploadFolder = "pets";

        // Act
        var result = GenerateUploadPath(fileName, uploadFolder);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().StartWith($"/uploads/{uploadFolder}/");
        result.Should().EndWith(".png");
        result.Should().Contain(fileName);
    }
    #endregion

    #region Helper Methods - File Validation Logic
    private static bool IsValidImageFile(byte[] fileBytes, string fileName)
    {
        return HasValidImageExtension(fileName) && HasValidImageMagicBytes(fileBytes);
    }

    private static bool HasValidImageExtension(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif" };

        return allowedExtensions.Contains(extension);
    }

    private static bool HasValidImageMagicBytes(byte[] fileBytes)
    {
        if (fileBytes == null || fileBytes.Length < 4)
            return false;

        // Check PNG magic bytes
        if (fileBytes.Length >= 8)
        {
            var pngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            if (fileBytes.Take(8).SequenceEqual(pngSignature))
                return true;
        }

        // Check JPEG magic bytes
        if (fileBytes.Length >= 2)
        {
            if (fileBytes[0] == 0xFF && fileBytes[1] == 0xD8)
                return true;
        }

        // Check GIF magic bytes
        if (fileBytes.Length >= 6)
        {
            var gif87a = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }; // GIF87a
            var gif89a = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }; // GIF89a

            if (fileBytes.Take(6).SequenceEqual(gif87a) || fileBytes.Take(6).SequenceEqual(gif89a))
                return true;
        }

        return false;
    }

    private static bool IsValidFileSize(int fileSize, int maxSizeInBytes)
    {
        return fileSize <= maxSizeInBytes;
    }

    private static string GenerateUniqueFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var uniqueId = Guid.NewGuid().ToString("N")[..12]; // First 12 characters
        return $"{uniqueId}{extension}";
    }

    private static string GenerateUploadPath(string fileName, string folder)
    {
        return $"/uploads/{folder}/{fileName}";
    }

    private static byte[] CreateTestImageBytes()
    {
        // Create a minimal valid PNG file (1x1 pixel transparent PNG)
        return new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x00, 0x00, 0x00, 0x0D, // IHDR chunk length
            0x49, 0x48, 0x44, 0x52, // IHDR
            0x00, 0x00, 0x00, 0x01, // Width: 1
            0x00, 0x00, 0x00, 0x01, // Height: 1
            0x08, 0x06, 0x00, 0x00, 0x00, // Bit depth, color type, compression, filter, interlace
            0x1F, 0x15, 0xC4, 0x89, // CRC
            0x00, 0x00, 0x00, 0x0A, // IDAT chunk length
            0x49, 0x44, 0x41, 0x54, // IDAT
            0x78, 0x9C, 0x62, 0x00, 0x00, 0x00, 0x02, 0x00, 0x01, // Compressed data
            0xE2, 0x21, 0xBC, 0x33, // CRC
            0x00, 0x00, 0x00, 0x00, // IEND chunk length
            0x49, 0x45, 0x4E, 0x44, // IEND
            0xAE, 0x42, 0x60, 0x82  // CRC
        };
    }

    private static byte[] CreateTestJpegBytes()
    {
        // Create a minimal valid JPEG file
        return new byte[]
        {
            0xFF, 0xD8, // JPEG SOI marker
            0xFF, 0xE0, // JFIF marker
            0x00, 0x10, // Length
            0x4A, 0x46, 0x49, 0x46, 0x00, // "JFIF\0"
            0x01, 0x01, // Version 1.1
            0x01, // Aspect ratio units (1 = no units)
            0x00, 0x01, // X density
            0x00, 0x01, // Y density
            0x00, 0x00, // Thumbnail width/height
            0xFF, 0xD9 // EOI marker
        };
    }
    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}