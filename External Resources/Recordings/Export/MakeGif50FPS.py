from PIL import Image
import os
import sys

def modify_gif_fps(file_path, min_duration=20):
    temp_path = "50fps_" + file_path

    # Open the GIF file
    with Image.open(file_path) as gif:
        # Verify it's a GIF
        if not gif.is_animated:
            raise ValueError("The provided file is not an animated GIF.")
        
        # Calculate the duration per frame for 50 fps (20 ms per frame)
        new_duration = max(20, min_duration)

        # Extract frames and modify their duration
        frames = []
        for frame in range(gif.n_frames):
            gif.seek(frame)
            frames.append(gif.copy())

        # Save a new GIF with the modified duration
        frames[0].save(
            temp_path,
            save_all=True,
            append_images=frames[1:],
            loop=gif.info.get("loop", 0),
            duration=new_duration
        )

    # Ensure the original file is closed, then replace it
    if os.path.exists(file_path):
        os.remove(file_path)
    os.rename(temp_path, file_path)

    print(f"Updated GIF to 50 fps and saved to: {file_path}")


if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Usage: python script.py <path_to_gif>")
        sys.exit(1)

    gif_path = sys.argv[1]
    modify_gif_fps(gif_path)