
import { useCallback, useEffect, useState } from 'react'
import './App.css'

function App() {
  const [activeTab, setActiveTab] = useState('student')
  const [classes, setClasses] = useState([])
  const [studentId, setStudentId] = useState(
    () => localStorage.getItem('studentId') || ''
  )
  const [holds, setHolds] = useState([])
  const [loading, setLoading] = useState(true)
  const [applying, setApplying] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [newClassName, setNewClassName] = useState('')
  const [newTotalSeats, setNewTotalSeats] = useState('')
  const [creatingClass, setCreatingClass] = useState(false)
  const [confirmingHoldId, setConfirmingHoldId] = useState(null)
  const [cancellingHoldId, setCancellingHoldId] = useState(null)
  const [currentTime, setCurrentTime] = useState(0)

 const loadClasses = useCallback(async () => {
  try {
    const response = await fetch('/classes')
    if (!response.ok) throw new Error('Unable to load admission classes.')

    const data = await response.json()
    setClasses(data)
  } catch (err) {
    setError(err.message)
  } finally {
    setLoading(false)
  }
}, [])

  const loadMyHolds = useCallback(async () => {
  if (!/^\d{17}$/.test(studentId)) return

  try {
    const response = await fetch('/me/holds', {
      headers: { 'X-Student-Id': studentId },
    })

    if (!response.ok) {
      throw new Error('Unable to retrieve your applications.')
    }

    const data = await response.json()
    setHolds(data)
  } catch (err) {
    setError(err.message)
  }
}, [studentId])

useEffect(() => {
  const initialLoad = setTimeout(loadClasses, 0)
  const interval = setInterval(loadClasses, 5000)

  return () => {
    clearTimeout(initialLoad)
    clearInterval(interval)
  }
}, [loadClasses])

useEffect(() => {
  if (!/^\d{17}$/.test(studentId)) return

  localStorage.setItem('studentId', studentId)

  const initialLoad = setTimeout(loadMyHolds, 0)
  const interval = setInterval(loadMyHolds, 5000)

  return () => {
    clearTimeout(initialLoad)
    clearInterval(interval)
  }
}, [studentId, loadMyHolds])

  useEffect(() => {
    const interval = setInterval(() => {
      setCurrentTime(Date.now())
    }, 1000)

    return () => clearInterval(interval)
  }, [])

  async function applyForAdmission(classId) {
    if (!/^\d{17}$/.test(studentId)) {
      setError('Please enter a valid 17-digit birth registration number.')
      return
    }

    setApplying(true)
    setError('')
    setSuccess('')

    try {
      const response = await fetch(`/classes/${classId}/holds`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Student-Id': studentId,
        },
      })

      const data = await response.json()

      if (!response.ok) {
        throw new Error(
          data.detail || data.title || 'Unable to apply for admission.'
        )
      }

      setSuccess('Your seat has been temporarily reserved!')
      await loadMyHolds()
      await loadClasses()
    } catch (err) {
      setError(err.message)
    } finally {
      setApplying(false)
    }
  }

  async function createAdmissionClass(event) {
    event.preventDefault()

    setError('')
    setSuccess('')

    if (newClassName.trim().length < 3) {
      setError('Class name must contain at least 3 characters.')
      return
    }

    const seatCapacity = Number(newTotalSeats)

    if (!Number.isInteger(seatCapacity) || seatCapacity < 1) {
      setError('Please enter a valid seat capacity.')
      return
    }

    setCreatingClass(true)

    try {
      const response = await fetch('/classes', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          name: newClassName.trim(),
          totalSeats: seatCapacity,
        }),
      })

      const data = await response.json()

      if (!response.ok) {
        throw new Error(
          data.detail || data.title || 'Unable to create admission class.'
        )
      }

      setSuccess('Admission class created successfully!')
      setNewClassName('')
      setNewTotalSeats('')

      await loadClasses()
    } catch (err) {
      setError(err.message)
    } finally {
      setCreatingClass(false)
    }
  }

  async function confirmAdmission(holdId) {
    setConfirmingHoldId(holdId)
    setError('')
    setSuccess('')

    try {
      const response = await fetch(`/holds/${holdId}/confirm`, {
        method: 'POST',
        headers: { 'X-Student-Id': studentId },
      })

      const data = await response.json()

      if (!response.ok) {
        throw new Error(
          data.detail || data.title || 'Unable to confirm admission.'
        )
      }

      setSuccess('Admission confirmed successfully!')
      await loadMyHolds()
      await loadClasses()
    } catch (err) {
      setError(err.message)
      await loadMyHolds()
    } finally {
      setConfirmingHoldId(null)
    }
  }

  async function cancelApplication(holdId) {
    const shouldCancel = window.confirm(
      'Are you sure you want to cancel this admission application?'
    )

    if (!shouldCancel) return

    setCancellingHoldId(holdId)
    setError('')
    setSuccess('')

    try {
      const response = await fetch(`/holds/${holdId}`, {
        method: 'DELETE',
        headers: { 'X-Student-Id': studentId },
      })

      const data = await response.json()

      if (!response.ok) {
        throw new Error(
          data.detail || data.title || 'Unable to cancel application.'
        )
      }

      setSuccess('Your admission application has been cancelled.')
      await loadMyHolds()
      await loadClasses()
    } catch (err) {
      setError(err.message)
      await loadMyHolds()
    } finally {
      setCancellingHoldId(null)
    }
  }

  function getRemainingSeconds(hold) {
    if (hold.status !== 'HELD') return 0

    return Math.max(
      0,
      Math.floor(
        (new Date(hold.expiresAt).getTime() - currentTime) / 1000
      )
    )
  }

  function formatTime(seconds) {
    const minutes = Math.floor(seconds / 60)
    const remaining = seconds % 60

    return `${String(minutes).padStart(2, '0')}:${String(remaining).padStart(2, '0')}`
  }

  return (
    <div className="app">
      <header className="navbar">
        <div className="brand">
          <div className="brand-icon">S</div>
          <div>
            <h2>School<span>Portal</span></h2>
            <p>ADMISSION MANAGEMENT SYSTEM</p>
          </div>
        </div>

        <nav>
          <button
            className={activeTab === 'student' ? 'nav-link active' : 'nav-link'}
            onClick={() => {
              setActiveTab('student')
              setError('')
              setSuccess('')
            }}
          >
            Student Portal
          </button>

          <button
            className={activeTab === 'admin' ? 'nav-link active' : 'nav-link'}
            onClick={() => {
              setActiveTab('admin')
              setError('')
              setSuccess('')
            }}
          >
            Admin Portal
          </button>
        </nav>
      </header>

      <main className="main-content">
        <section className="welcome-section">
          <div>
            <span className="eyebrow">ACADEMIC SESSION 2026–2027</span>

            <h1>
              {activeTab === 'student'
                ? 'Your future starts here.'
                : 'Admission overview.'}
            </h1>

            <p>
              {activeTab === 'student'
                ? 'Explore available classes and secure your admission through our online process.'
                : 'Manage admission classes and monitor seat availability.'}
            </p>
          </div>

          <div className="session-card">
            <span>ADMISSION SESSION</span>
            <strong>2026 – 2027</strong>
            <small>Online admission portal</small>
          </div>
        </section>

        {activeTab === 'student' && (
          <>
            <section className="content-section">
              <div className="section-heading">
                <div>
                  <span className="eyebrow">STUDENT IDENTIFICATION</span>
                  <h2>Enter your birth registration number</h2>
                </div>
              </div>

              <div className="student-form">
                <input
                  type="text"
                  value={studentId}
                  maxLength={17}
                  placeholder="Enter your 17-digit number"
                  onChange={event => {
                    setStudentId(
                      event.target.value.replace(/\D/g, '').slice(0, 17)
                    )
                    setError('')
                    setSuccess('')
                  }}
                />

                <span>{studentId.length}/17 digits</span>
              </div>

              {error && <p className="error-message">{error}</p>}
              {success && <p className="success-message">{success}</p>}
            </section>

            <section className="content-section">
              <div className="section-heading">
                <div>
                  <span className="eyebrow">EXPLORE OPPORTUNITIES</span>
                  <h2>Available admission classes</h2>
                </div>

                <button className="refresh-button" onClick={loadClasses}>
                  ↻ Refresh
                </button>
              </div>

              {loading && (
                <p className="message">Loading admission classes...</p>
              )}

              <div className="class-grid">
                {classes.map(item => (
                  <article className="class-card" key={item.id}>
                    <div className="class-card-top">
                      <span className="class-label">ADMISSION OPEN</span>
                      <span className="class-icon">✦</span>
                    </div>

                    <h3>{item.name}</h3>
                    <p>Academic session 2026–2027</p>

                    <div className="seat-info">
                      <span>Total seats</span>
                      <strong>{item.totalSeats}</strong>
                    </div>

                    <div className="seat-info">
                      <span>Available seats</span>
                      <strong>{item.availableSeats ?? '—'}</strong>
                    </div>

                    <button
                      className="apply-button"
                      disabled={
                        applying ||
                        item.availableSeats === 0 ||
                        !/^\d{17}$/.test(studentId)
                      }
                      onClick={() => applyForAdmission(item.id)}
                    >
                      {applying ? 'Processing...' : 'Apply for Admission'}
                    </button>
                  </article>
                ))}
              </div>

              {!loading && classes.length === 0 && (
                <div className="empty-state">
                  <h3>No admission classes available</h3>
                  <p>Please check again later.</p>
                </div>
              )}
            </section>

            <section className="content-section">
              <div className="section-heading">
                <div>
                  <span className="eyebrow">APPLICATION TRACKING</span>
                  <h2>My admission applications</h2>
                </div>
              </div>

              {!/^\d{17}$/.test(studentId) ? (
                <div className="empty-state">
                  Enter your 17-digit number to view your applications.
                </div>
              ) : holds.length === 0 ? (
                <div className="empty-state">
                  No applications found for this student.
                </div>
              ) : (
                <div className="class-grid">
                  {holds.map(hold => {
                    const remaining = getRemainingSeconds(hold)

                    return (
                      <article className="class-card" key={hold.id}>
                        <h3>Admission application</h3>

                        <p>Status: <strong>{hold.status}</strong></p>

                        {hold.status === 'HELD' && remaining > 0 && (
                          <>
                            <div className="countdown">
                              Time remaining: {formatTime(remaining)}
                            </div>

                            <button
                              className="apply-button"
                              disabled={confirmingHoldId === hold.id}
                              onClick={() => confirmAdmission(hold.id)}
                            >
                              {confirmingHoldId === hold.id
                                ? 'Processing Payment...'
                                : 'Simulate Payment & Confirm'}
                            </button>

                            <button
                              className="cancel-button"
                              disabled={cancellingHoldId === hold.id}
                              onClick={() => cancelApplication(hold.id)}
                            >
                              {cancellingHoldId === hold.id
                                ? 'Cancelling...'
                                : 'Cancel Application'}
                            </button>
                          </>
                        )}

                        {hold.status === 'HELD' && remaining === 0 && (
                          <p className="error-message">
                            Your reservation has expired. Updating status...
                          </p>
                        )}

                        {hold.status === 'CONFIRMED' && (
                          <p className="success-message">
                            Your admission is confirmed.
                          </p>
                        )}

                        {hold.status === 'EXPIRED' && (
                          <p className="error-message">
                            This reservation expired.
                          </p>
                        )}

                        {hold.status === 'CANCELLED' && (
                          <p className="message">
                            This application was cancelled.
                          </p>
                        )}
                      </article>
                    )
                  })}
                </div>
              )}
            </section>
          </>
        )}

        {activeTab === 'admin' && (
          <>
            <section className="content-section">
              <div className="section-heading">
                <div>
                  <span className="eyebrow">ADMINISTRATION</span>
                  <h2>Create admission class</h2>
                </div>
              </div>

              <form className="admin-form" onSubmit={createAdmissionClass}>
                <div className="form-field">
                  <label>Admission class name</label>
                  <input
                    type="text"
                    placeholder="e.g. Class 8 - Morning Shift"
                    value={newClassName}
                    onChange={event => setNewClassName(event.target.value)}
                    maxLength={100}
                    required
                  />
                </div>

                <div className="form-field">
                  <label>Total seat capacity</label>
                  <input
                    type="number"
                    placeholder="Enter total seats"
                    value={newTotalSeats}
                    onChange={event => setNewTotalSeats(event.target.value)}
                    min="1"
                    required
                  />
                </div>

                <button
                  className="apply-button"
                  type="submit"
                  disabled={creatingClass}
                >
                  {creatingClass
                    ? 'Creating...'
                    : '+ Create Admission Class'}
                </button>
              </form>

              {error && <p className="error-message">{error}</p>}
              {success && <p className="success-message">{success}</p>}
            </section>

            <section className="content-section">
              <div className="section-heading">
                <div>
                  <span className="eyebrow">LIVE MONITORING</span>
                  <h2>Admission seat overview</h2>
                </div>

                <button className="refresh-button" onClick={loadClasses}>
                  ↻ Refresh
                </button>
              </div>

              <div className="admin-stats">
                <div className="stat-card">
                  <span>Total classes</span>
                  <strong>{classes.length}</strong>
                </div>

                <div className="stat-card">
                  <span>Total seats</span>
                  <strong>
                    {classes.reduce(
                      (sum, item) => sum + item.totalSeats,
                      0
                    )}
                  </strong>
                </div>

                <div className="stat-card">
                  <span>Confirmed seats</span>
                  <strong>
                    {classes.reduce(
                      (sum, item) => sum + (item.confirmedSeats || 0),
                      0
                    )}
                  </strong>
                </div>

                <div className="stat-card">
                  <span>Available seats</span>
                  <strong>
                    {classes.reduce(
                      (sum, item) => sum + (item.availableSeats || 0),
                      0
                    )}
                  </strong>
                </div>
              </div>

              <div className="admin-class-list">
                {classes.map(item => (
                  <article className="admin-class-card" key={item.id}>
                    <div className="admin-class-heading">
                      <h3>{item.name}</h3>
                      <span>{item.totalSeats} total seats</span>
                    </div>

                    <div className="admin-seat-grid">
                      <div>
                        <span>Held</span>
                        <strong>{item.heldSeats ?? 0}</strong>
                      </div>

                      <div>
                        <span>Confirmed</span>
                        <strong>{item.confirmedSeats ?? 0}</strong>
                      </div>

                      <div>
                        <span>Available</span>
                        <strong>{item.availableSeats ?? 0}</strong>
                      </div>
                    </div>

                    <div className="seat-progress">
                      <div
                        style={{
                          width: `${
                            item.totalSeats > 0
                              ? ((item.totalSeats - item.availableSeats) /
                                  item.totalSeats) *
                                100
                              : 0
                          }%`,
                        }}
                      />
                    </div>
                  </article>
                ))}
              </div>
            </section>
          </>
        )}
      </main>

      <footer>
        <p>© 2026 School Admission System. All rights reserved.</p>
      </footer>
    </div>
  )
}

export default App
