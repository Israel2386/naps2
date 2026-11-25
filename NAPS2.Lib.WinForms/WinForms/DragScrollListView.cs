using System.Drawing;
using System.Windows.Forms;
using NAPS2.Platform.Windows;

namespace NAPS2.WinForms;

public class DragScrollListView : ListView
{
    // From http://stackoverflow.com/questions/660663/c-sharp-implementing-auto-scroll-in-a-listview-while-drag-dropping

    private Timer _tmrLvScroll;
    private System.ComponentModel.IContainer components;
    private int _mintScrollDirection;

    const int WM_VSCROLL = 277; // Vertical scroll
    const int SB_LINEUP = 0; // Scrolls one line up
    const int SB_LINEDOWN = 1; // Scrolls one line down

    public Func<Point, bool>? HandleCustomMouseDown;
    public Func<Point, bool>? HandleCustomMouseMove;
    public Func<Point, bool>? HandleCustomMouseUp;

    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_MOUSEMOVE = 0x0200;
    private const int WM_SETCURSOR = 0x0020;

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_SETCURSOR && (m.LParam.ToInt64() & 0xFFFF) == 1) // 1 = HTCLIENT
        {
            Cursor.Current = this.Cursor;
            m.Result = (IntPtr)1;
            return;
        }
        else if (m.Msg == WM_LBUTTONDOWN && HandleCustomMouseDown != null)
        {
            var pt = GetPointFromLParam(m.LParam);
            if (HandleCustomMouseDown(pt))
            {
                Focus();
                return; // Bloquea el lazo de selección y el ItemDrag nativo de Windows
            }
        }
        else if (m.Msg == WM_MOUSEMOVE && HandleCustomMouseMove != null)
        {
            var pt = GetPointFromLParam(m.LParam);
            if (HandleCustomMouseMove(pt))
            {
                return;
            }
        }
        else if (m.Msg == WM_LBUTTONUP && HandleCustomMouseUp != null)
        {
            var pt = GetPointFromLParam(m.LParam);
            if (HandleCustomMouseUp(pt))
            {
                return;
            }
        }

        base.WndProc(ref m);
    }

    private static Point GetPointFromLParam(IntPtr lParam)
    {
        int x = (short)(lParam.ToInt64() & 0xFFFF);
        int y = (short)((lParam.ToInt64() >> 16) & 0xFFFF);
        return new Point(x, y);
    }
    public DragScrollListView()
    {
        components = new System.ComponentModel.Container();
        _tmrLvScroll = new Timer(components);
        SuspendLayout();
        _tmrLvScroll.Tick += tmrLVScroll_Tick;
        HandleDestroyed += (_, _) => _tmrLvScroll.Dispose();
        DragOver += ListViewBase_DragOver;
        ResumeLayout(false);
    }

    private int EdgeSize => Font.Height;

    private void ListViewBase_DragOver(object? sender, DragEventArgs e)
    {
        Point position = PointToClient(new Point(e.X, e.Y));

        if (position.Y <= EdgeSize)
        {
            // getting close to top, ensure previous item is visible
            _mintScrollDirection = SB_LINEUP;
            _tmrLvScroll.Enabled = true;
        }
        else if (position.Y >= ClientSize.Height - EdgeSize)
        {
            // getting close to bottom, ensure next item is visible
            _mintScrollDirection = SB_LINEDOWN;
            _tmrLvScroll.Enabled = true;
        }
        else
        {
            _tmrLvScroll.Enabled = false;
        }
    }

    private void tmrLVScroll_Tick(object? sender, EventArgs e)
    {
        Win32.SendMessage(Handle, WM_VSCROLL, (IntPtr)_mintScrollDirection, IntPtr.Zero);
    }
}