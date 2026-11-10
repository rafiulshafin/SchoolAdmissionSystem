
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, cleanup } from '@testing-library/react'
import App from './App'

const sampleClass = {
  id: 'class-1',
  name: 'Class 6 - Morning Shift',
  totalSeats: 30,
  heldSeats: 2,
  confirmedSeats: 3,
  availableSeats: 25,
}

function mockResponse(data, ok = true) {
  return {
    ok,
    json: async () => data,
  }
}

describe('School Admission System', () => {
  let fetchMock

  beforeEach(() => {
    localStorage.clear()

    fetchMock = vi.fn().mockResolvedValue(
      mockResponse([sampleClass])
    )

    vi.stubGlobal('fetch', fetchMock)
  })

  afterEach(() => {
    cleanup()
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('displays admission classes and seat availability', async () => {
    render(<App />)

    expect(
      await screen.findByText('Class 6 - Morning Shift')
    ).toBeInTheDocument()

    expect(screen.getByText('Available seats')).toBeInTheDocument()
    expect(screen.getByText('25')).toBeInTheDocument()
  })

  it('disables admission application for an invalid student ID', async () => {
    render(<App />)

    await screen.findByText('Class 6 - Morning Shift')

    const applyButton = screen.getByRole('button', {
      name: 'Apply for Admission',
    })

    expect(applyButton).toBeDisabled()
  })

  it('allows an admin to create an admission class', async () => {
    const newClass = {
      id: 'class-2',
      name: 'Class 7 - Morning Shift',
      totalSeats: 20,
      heldSeats: 0,
      confirmedSeats: 0,
      availableSeats: 20,
    }

    let classes = [sampleClass]

    fetchMock.mockImplementation(async (url, options = {}) => {
      if (url === '/classes' && options.method === 'POST') {
        classes = [...classes, newClass]
        return mockResponse(newClass)
      }

      if (url === '/classes') {
        return mockResponse(classes)
      }

      return mockResponse([])
    })

    render(<App />)

    await screen.findByText('Class 6 - Morning Shift')

    fireEvent.click(
      screen.getByRole('button', { name: 'Admin Portal' })
    )

    fireEvent.change(
      screen.getByPlaceholderText('e.g. Class 8 - Morning Shift'),
      { target: { value: 'Class 7 - Morning Shift' } }
    )

    fireEvent.change(
      screen.getByPlaceholderText('Enter total seats'),
      { target: { value: '20' } }
    )

    fireEvent.click(
      screen.getByRole('button', { name: '+ Create Admission Class' })
    )

    expect(
      await screen.findByText('Admission class created successfully!')
    ).toBeInTheDocument()

    expect(
      await screen.findByText('Class 7 - Morning Shift')
    ).toBeInTheDocument()

    expect(fetchMock).toHaveBeenCalledWith(
      '/classes',
      expect.objectContaining({ method: 'POST' })
    )
  })
})
