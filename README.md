# LibLog
_Software Dev. Senior Project_
<img width="1354" height="603" alt="liblog_1" src="https://github.com/user-attachments/assets/2f51ca2c-b7c6-42dd-a38b-721677d966c0" />
LibLog is a Windows native library cataloguing application. It was created during the Fall 2025 semester for my senior project. 
<br/>
### Tech Stack
Front End: WinUI 3 (XAML) \
Back End: --> C#, SQLite \
API: OpenLibrary API \
<br/>
### How does it work?
Users can scan a physical barcode of the book they wish to add to their library, or manually enter the ISBN of the book. The information for the book will populate and allow the user to confirm whether that is the book they are looking for. After confirmation, the book will appear in the user's library with the cover image, title, and author visible. There is a custom tagging system which can be used for sorting/filtering the books in the library. Lastly, there are default sorting options and a search function, as well as the ability to go directly to OpenLibrary's website. 
<br/>
### Limitations
The main limitation of this app is that only books that are available on OpenLibrary can be added to a library. The website offers _millions_ of books but it is not guaranteed that the desired book will be available. Secondly, the app is not perfect (I know shocker). There are some bugs and I'm not completely satisfied with the layout of the UI, but it does function as intended. 
